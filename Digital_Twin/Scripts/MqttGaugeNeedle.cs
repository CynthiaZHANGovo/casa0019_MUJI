using System;
using System.Text;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MqttGaugeNeedle : MonoBehaviour
{
  // MQTT broker connection settings
  public string brokerAddress = "mqtt.cetools.org";
  public int brokerPort = 1884;
  public string username = "student";
  public string password = "ce2021-mqtt-forget-whale";
  public string topic = "student/MUJI/qingshan/walk";

  // How fast the needle moves toward its target rotation
  public float smoothSpeed = 5f;

  private MqttClient client;
  private Vector3 baseEuler;
  private float targetOffsetY;
  private float currentOffsetY;

  private const int fixedSeg = 8;

  void Start()
  {
    // Store the baseline rotation so all transforms are relative
    baseEuler = transform.localEulerAngles;

    // Initialise needle angle from a fixed lookup segment
    targetOffsetY = 54.625f - fixedSeg * 4.75f;
    currentOffsetY = targetOffsetY;

    transform.localEulerAngles = baseEuler + new Vector3(0f, currentOffsetY, 0f);

    try
    {
      // Connect to MQTT broker (no SSL)
      client = new MqttClient(brokerAddress, brokerPort, false, null, null, MqttSslProtocols.None);
      client.MqttMsgPublishReceived += OnFakeMqttMessage;

      string clientId = Guid.NewGuid().ToString();
      client.Connect(clientId, username, password);

      client.Subscribe(
          new string[] { topic },
          new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }
      );
    }
    catch { }
  }

  // Called whenever a message is published on the subscribed topic
  private void OnFakeMqttMessage(object sender, MqttMsgPublishEventArgs e)
  {
    string msg = Encoding.UTF8.GetString(e.Message).Trim();
    Debug.Log("[MqttGaugeNeedle] MQTT received (IGNORED): " + msg);
  }

  void Update()
  {
    // Smoothly interpolate toward the target rotation
    currentOffsetY = Mathf.Lerp(
        currentOffsetY,
        targetOffsetY,
        Time.deltaTime * smoothSpeed
    );

    transform.localEulerAngles = baseEuler + new Vector3(0f, currentOffsetY, 0f);
  }

  private void OnDestroy()
  {
    if (client != null && client.IsConnected)
      client.Disconnect();
  }
}
