using UnityEngine;
using UnityEngine.UI;          // Used by ScrollRect
using TMPro;
using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


// ====== Simple data structs for JSON parsing ======
[Serializable]
public class BusArrival
{
  public string route;
  public string arrival_london_aquatics;
  public string arrival_stratford_city;
  public bool recommended;
}

[Serializable]
public class BusArrivalListWrapper
{
  public BusArrival[] items;
}
// ===================================================

public class MqttTextDisplay : MonoBehaviour
{
  [Header("UI")]
  public TextMeshProUGUI targetText;     // Linked TextMeshPro field
  public ScrollRect scrollRect;          // ScrollRect component on Scroll View
  public float autoScrollSpeed = 0.05f;  // Auto-scroll speed (larger = faster)

  [Header("MQTT Config")]
  public string brokerAddress = "mqtt.cetools.org";
  public int brokerPort = 1884;
  public string username = "student";
  public string password = "ce2021-mqtt-forget-whale";
  public string topic = "student/MUJI/qingshan/bus-108";

  private MqttClient client;
  private string latestMessage = "";
  private bool hasNewMessage = false;

  // Auto-scroll helper
  private float scrollPos = 1f; // 1 = top, 0 = bottom

  void Start()
  {
    if (targetText == null)
      targetText = GetComponent<TextMeshProUGUI>();

    try
    {
      Debug.Log("Connecting to MQTT ...");

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

      Debug.Log("MQTT Connected!");

      client.Subscribe(
          new string[] { topic },
          new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }
      );

      targetText.text = $"Connected\nSubscribing: {topic}";
    }
    catch (Exception e)
    {
      Debug.LogError("MQTT connection failed: " + e);
      targetText.text = "MQTT connect failed";
    }
  }

  // Callback running in background thread when MQTT message arrives
  private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
  {
    latestMessage = System.Text.Encoding.UTF8.GetString(e.Message);
    hasNewMessage = true;
  }

  void Update()
  {
    // Update UI when new message arrives
    if (hasNewMessage)
    {
      targetText.text = FormatBusTimetable(latestMessage);
      hasNewMessage = false;

      // Reset scroll to top each time new content appears
      scrollPos = 1f;
      if (scrollRect != null)
        scrollRect.verticalNormalizedPosition = 1f;
    }

    // Auto-scrolling (if content fits, scrolling may look inactive)
    if (scrollRect != null && targetText != null)
    {
      scrollPos -= autoScrollSpeed * Time.deltaTime;
      if (scrollPos < 0f)
        scrollPos = 1f;  // Loop back to top once reaching bottom

      scrollRect.verticalNormalizedPosition = scrollPos;
    }
  }

  // Format JSON string into readable text lines
  string FormatBusTimetable(string json)
  {
    if (string.IsNullOrEmpty(json))
      return "No data";

    try
    {
      // JsonUtility cannot parse top-level arrays, so wrap it
      string wrapped = "{\"items\":" + json + "}";

      BusArrivalListWrapper list =
          JsonUtility.FromJson<BusArrivalListWrapper>(wrapped);

      if (list == null || list.items == null || list.items.Length == 0)
        return "No arrivals";

      StringBuilder sb = new StringBuilder();

      string routeName = list.items[0].route;
      sb.AppendLine($"ROUTE {routeName}");
      sb.AppendLine(""); // Empty line

      // ======== Header: three columns ========
      sb.AppendLine(" Aquatics    Stratford    Rec");
      sb.AppendLine("--------------------------------");

      // ======== Table body rows ========
      foreach (var item in list.items)
      {
        string star = item.recommended ? "★" : "";

        // ,-8 = left align in 8 chars width
        sb.AppendLine(
          string.Format("{0,-10}{1,-12}{2}",
            item.arrival_london_aquatics,
            item.arrival_stratford_city,
            star
          )
        );
      }

      return sb.ToString();
    }
    catch (Exception e)
    {
      Debug.LogError("JSON parse error: " + e + "\nRAW:\n" + json);
      return "DATA ERROR";
    }
  }

}
