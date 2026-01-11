using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Bus API manager - fetches and parses real API data
/// </summary>
public class BusAPIManager : MonoBehaviour
{
    [Header("========== API Configuration ==========")]
    public string route339ApiUrl = "http://10.129.111.26:3000/api/timetable/339";
    public string route108ApiUrl = "http://10.129.111.26:3000/api/timetable/108";
    public string apiKey = "";
    public int timeoutSeconds = 10;
    
    [Header("========== Update Settings ==========")]
    public float updateInterval = 30f;
    public bool autoUpdate = true;
    
    [Header("========== Debug Options ==========")]
    public bool showDetailedLogs = true;
    public bool useMockData = false;
    
    public event Action<CompleteBusData> OnBusDataUpdated;
    public event Action<string> OnBusDataError;
    
    private CompleteBusData cachedBusData;
    private bool isUpdating = false;
    
    void Start()
    {
        // Fix SSL validation errors for HTTP requests
        System.Net.ServicePointManager.ServerCertificateValidationCallback =
            delegate { return true; };
        
        Debug.Log("==========================================");
        Debug.Log("  Bus API Manager Initialized");
        Debug.Log("==========================================");
        Debug.Log("Route 339 API: " + route339ApiUrl);
        Debug.Log("Route 108 API: " + route108ApiUrl);
        Debug.Log("==========================================");
        
        cachedBusData = new CompleteBusData
        {
            route339 = new List<BusArrivalItem>(),
            route108 = new List<BusArrivalItem>()
        };
        
        StartCoroutine(UpdateBusData());
        
        if (autoUpdate)
        {
            StartCoroutine(AutoUpdateRoutine());
        }
    }
    
    public void RefreshData()
    {
        if (!isUpdating)
        {
            StartCoroutine(UpdateBusData());
        }
    }
    
    public CompleteBusData GetCachedData()
    {
        return cachedBusData;
    }
    
    IEnumerator AutoUpdateRoutine()
    {
        while (autoUpdate)
        {
            yield return new WaitForSeconds(updateInterval);
            yield return StartCoroutine(UpdateBusData());
        }
    }
    
    IEnumerator UpdateBusData()
    {
        if (isUpdating)
        {
            yield break;
        }
        
        isUpdating = true;
        Log("Updating bus data...");
        
        if (useMockData)
        {
            Log("Using mock data");
            GenerateMockData();
            isUpdating = false;
            yield break;
        }
        
        bool success339 = false;
        bool success108 = false;
        
        yield return StartCoroutine(FetchRoute339Data((success) => success339 = success));
        yield return StartCoroutine(FetchRoute108Data((success) => success108 = success));
        
        if (success339 || success108)
        {
            CalculateNextBus();
            
            OnBusDataUpdated?.Invoke(cachedBusData);
            Log("✓ Bus data update complete");
        }
        else
        {
            LogError("API request failed, falling back to mock data");
            GenerateMockData();
            OnBusDataError?.Invoke("API request failed");
        }
        
        isUpdating = false;
    }
    
    IEnumerator FetchRoute339Data(Action<bool> callback)
    {
        Log("→ Requesting route 339: " + route339ApiUrl);
        
        using (UnityWebRequest request = UnityWebRequest.Get(route339ApiUrl))
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }
            
            request.timeout = timeoutSeconds;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Log("✓ Route 339 response received");
                Log("JSON first 100 chars: " + (json.Length > 100 ? json.Substring(0, 100) : json));
                
                callback?.Invoke(ParseBusData(json, cachedBusData.route339, "339"));
            }
            else
            {
                LogError("✗ Route 339 request failed: " + request.error);
                callback?.Invoke(false);
            }
        }
    }
    
    IEnumerator FetchRoute108Data(Action<bool> callback)
    {
        Log("→ Requesting route 108: " + route108ApiUrl);
        
        using (UnityWebRequest request = UnityWebRequest.Get(route108ApiUrl))
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }
            
            request.timeout = timeoutSeconds;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Log("✓ Route 108 response received");
                Log("JSON first 100 chars: " + (json.Length > 100 ? json.Substring(0, 100) : json));
                
                callback?.Invoke(ParseBusData(json, cachedBusData.route108, "108"));
            }
            else
            {
                LogError("✗ Route 108 request failed: " + request.error);
                callback?.Invoke(false);
            }
        }
    }
    
    bool ParseBusData(string json, List<BusArrivalItem> targetList, string routeNumber)
    {
        try
        {
            Log("========== Parsing data for route " + routeNumber + " ==========");
            
            BusArrivalAPIItem[] apiItems = JsonHelper.FromJson<BusArrivalAPIItem>(json);
            
            if (apiItems == null || apiItems.Length == 0)
            {
                LogError("Parse failed: empty data");
                return false;
            }
            
            Log("Received " + apiItems.Length + " records");
            
            targetList.Clear();
            DateTime now = DateTime.Now;
            
            int addedCount = 0;
            foreach (var apiItem in apiItems)
            {
                DateTime arrivalTime = TimeConverter.ParseTimeToday(apiItem.arrival_london_aquatics);
                
                if (arrivalTime > now)
                {
                    int minutesUntil = TimeConverter.GetMinutesUntil(arrivalTime);
                    
                    BusArrivalItem item = new BusArrivalItem
                    {
                        departureTime = apiItem.arrival_london_aquatics,
                        stopName = "UCL EAST / E",
                        minutesUntilArrival = minutesUntil,
                        status = apiItem.recommended ? "RECOMMENDED" : "ON TIME",
                        destination = "TO STRATFORD"
                    };
                    
                    targetList.Add(item);
                    addedCount++;
                    
                    if (addedCount == 1)
                    {
                        Log("Next bus: " + item.departureTime + " (" + minutesUntil + " mins)");
                    }
                    
                    if (addedCount >= 5) break;
                }
            }
            
            if (addedCount == 0)
            {
                LogError("No future buses found");
                return false;
            }
            
            Log("✓ Added " + addedCount + " upcoming buses");
            Log("==========================================");
            return true;
        }
        catch (Exception e)
        {
            LogError("Parse exception: " + e.Message);
            LogError("JSON content: " + json);
            return false;
        }
    }
    
    void CalculateNextBus()
    {
        DateTime now = DateTime.Now;
        DateTime? nextBusTime = null;
        string nextRoute = "";
        
        if (cachedBusData.route339.Count > 0)
        {
            DateTime t = TimeConverter.ParseTimeToday(cachedBusData.route339[0].departureTime);
            nextBusTime = t;
            nextRoute = "339";
        }
        
        if (cachedBusData.route108.Count > 0)
        {
            DateTime t = TimeConverter.ParseTimeToday(cachedBusData.route108[0].departureTime);
            
            if (!nextBusTime.HasValue || t < nextBusTime.Value)
            {
                nextBusTime = t;
                nextRoute = "108";
            }
        }
        
        if (nextBusTime.HasValue)
        {
            cachedBusData.nextBusTime = nextBusTime.Value;
            cachedBusData.nextBusRoute = nextRoute;
            
            cachedBusData.latestDepartureTime = nextBusTime.Value.AddMinutes(-5);
            
            Log("==========================================");
            Log("  Next Bus");
            Log("==========================================");
            Log("Route: " + nextRoute);
            Log("Arrival time: " + nextBusTime.Value.ToString("HH:mm"));
            Log("Temporary depart time: " + cachedBusData.latestDepartureTime.ToString("HH:mm"));
            Log("(Actual depart time will use walkingTimeMinutes from weather API)");
            Log("==========================================");
        }
    }
    
    void GenerateMockData()
    {
        DateTime now = DateTime.Now;
        
        cachedBusData.route339.Clear();
        for (int i = 0; i < 5; i++)
        {
            int minutes = 5 + i * 15;
            DateTime time = now.AddMinutes(minutes);
            cachedBusData.route339.Add(new BusArrivalItem
            {
                departureTime = time.ToString("HH:mm"),
                stopName = "UCL EAST / E",
                minutesUntilArrival = minutes,
                status = "ON TIME",
                destination = "TO STRATFORD"
            });
        }
        
        cachedBusData.route108.Clear();
        for (int i = 0; i < 5; i++)
        {
            int minutes = 8 + i * 20;
            DateTime time = now.AddMinutes(minutes);
            cachedBusData.route108.Add(new BusArrivalItem
            {
                departureTime = time.ToString("HH:mm"),
                stopName = "UCL EAST / E",
                minutesUntilArrival = minutes,
                status = "ON TIME",
                destination = "TO STRATFORD"
            });
        }
        
        CalculateNextBus();
        
        OnBusDataUpdated?.Invoke(cachedBusData);
    }
    
    void Log(string message)
    {
        if (showDetailedLogs)
            Debug.Log("[BusAPI] " + message);
    }
    
    void LogError(string message)
    {
        Debug.LogError("[BusAPI] " + message);
    }
}
