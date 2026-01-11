using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Authentication;
using UnityEngine;
using UnityEngine.UI;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


// =====================
// JSON data structures
// =====================
[Serializable]
public class WalkHistoryPayload
{
  public int days;
  public List<WalkHistoryItem> items;
}

[Serializable]
public class WalkHistoryItem
{
  public string date;
  public float avgTemperatureC;
  public string condition;
  public float walkingTimeMinutes;
  public int walkingTimeSeconds;
  public int weatherIndex;
  public int tempBucketIndex;
  public int segmentIndex;
}


// =====================
// Chart + MQTT main script
// =====================
public class WalkHistoryMqttChart : MonoBehaviour
{
  [Header("UI Chart Settings")]
  public RectTransform barContainer;   // Parent object that holds bars (BarsContainer)
  public GameObject barPrefab;         // Prefab for a single bar

  public float barSpacing = 4f;        // Spacing between bars
  public float maxBarHeight = 120f;    // Maximum height of a bar

  [Header("Temperature Gradient Colors")]
  public Color coldColor = new Color(0.6f, 0.8f, 1f);  // Cold color
  public Color hotColor = new Color(1f, 0.5f, 0.3f);   // Hot color


  [Header("MQTT Config")]
  public string brokerAddress = "mqtt.cetools.org";
  public int brokerPort = 1884;
  public string username = "student";
  public string password = "ce2021-mqtt-forget-whale";

  public string topic = "student/MUJI/qingshan/walk-history";

  private MqttClient client;

  // Thread safe data flags
  private WalkHistoryPayload pendingHistory;
  private bool hasNewHistory = false;
  private readonly object lockObj = new object();

  // Store current bars so we can clear them later
  private readonly List<GameObject> bars = new List<GameObject>();


  // =====================
  // Start: Connect to MQTT
  // =====================
  void Start()
  {
    try
    {
      Debug.Log("[WalkHistory] Connecting MQTT...");

      // ★ Identical MQTT constructor usage as other working scripts in your project
      client = new MqttClient(
          brokerAddress,
          brokerPort,
          false,
          null,
          null,
          MqttSslProtocols.None
      );

      client.MqttMsgPublishReceived += OnMqttMessageReceived;

      string clientID = Guid.NewGuid().ToString();
      client.Connect(clientID, username, password);

      client.Subscribe(
          new string[] { topic },
          new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }
      );

      Debug.Log("[WalkHistory] Subscribed to: " + topic);
    }
    catch (Exception e)
    {
      Debug.LogError("[WalkHistory ERROR] MQTT connection failed: " + e);
    }
  }


  // =====================
  // MQTT callback (NOT running on main thread)
  // =====================
  private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
  {
    string msg = Encoding.UTF8.GetString(e.Message);
    Debug.Log("[WalkHistory] Payload: " + msg);

    try
    {
      WalkHistoryPayload history = JsonUtility.FromJson<WalkHistoryPayload>(msg);

      lock (lockObj)
      {
        pendingHistory = history;
        hasNewHistory = true;
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("[WalkHistory] JSON parse error: " + ex.Message);
    }
  }


  // =====================
  // Update: Main thread processes incoming data
  // =====================
  void Update()
  {
    if (hasNewHistory)
    {
      WalkHistoryPayload copy;

      lock (lockObj)
      {
        copy = pendingHistory;
        hasNewHistory = false;
      }

      if (copy != null && copy.items != null && copy.items.Count > 0)
      {
        RedrawChart(copy.items);
      }
    }
  }


  // =====================
  // Draw bar chart
  // =====================
  private void RedrawChart(List<WalkHistoryItem> items)
  {
    // 1. Clear existing bars
    foreach (var bar in bars)
    {
      Destroy(bar);
    }
    bars.Clear();

    if (barContainer == null || barPrefab == null)
    {
      Debug.LogError("[WalkHistory] Missing barContainer or barPrefab!");
      return;
    }

    int count = items.Count;
    if (count == 0) return;

    // 2. Find max walking time + temp range for scaling
    float maxWalk = 0f;
    float minTemp = float.MaxValue;
    float maxTemp = float.MinValue;

    foreach (var i in items)
    {
      if (i.walkingTimeMinutes > maxWalk)
        maxWalk = i.walkingTimeMinutes;

      if (i.avgTemperatureC < minTemp)
        minTemp = i.avgTemperatureC;

      if (i.avgTemperatureC > maxTemp)
        maxTemp = i.avgTemperatureC;
    }

    if (maxWalk <= 0f) maxWalk = 1f;
    if (Mathf.Approximately(minTemp, maxTemp)) maxTemp = minTemp + 0.01f;

    // 3. Compute width of each bar
    float containerWidth = barContainer.rect.width;
    float totalSpacing = barSpacing * (count - 1);

    float barWidth = (containerWidth - totalSpacing) / count;
    barWidth = Mathf.Max(2f, barWidth);

    // 4. Instantiate bars one by one
    for (int i = 0; i < count; i++)
    {
      var item = items[i];

      GameObject barObj = Instantiate(barPrefab, barContainer);
      RectTransform rt = barObj.GetComponent<RectTransform>();

      // Height = normalized walking time × max height
      float hNorm = item.walkingTimeMinutes / maxWalk;
      float height = hNorm * maxBarHeight;

      rt.sizeDelta = new Vector2(barWidth, height);

      // X position
      float x = i * (barWidth + barSpacing);
      rt.anchoredPosition = new Vector2(x, height * 0.5f);

      // Color = temperature based gradient
      float t = Mathf.InverseLerp(minTemp, maxTemp, item.avgTemperatureC);
      Color c = Color.Lerp(coldColor, hotColor, t);

      var img = barObj.GetComponent<Image>();
      if (img != null)
        img.color = c;

      bars.Add(barObj);
    }

    Debug.Log("[WalkHistory] Chart updated. Items: " + count);
  }


  void OnDestroy()
  {
    if (client != null && client.IsConnected)
      client.Disconnect();
  }
}
