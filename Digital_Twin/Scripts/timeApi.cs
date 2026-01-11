using UnityEngine;
using TMPro;
using System;

public class TimeDisplay : MonoBehaviour
{
  [Header("UI Component")]
  public TextMeshProUGUI timeText;

  [Header("Time Format (Editable)")]
  public string timeFormat = "HH:mm:ss";
  // Example: "HH:mm" -> 23:45
  //          "hh:mm tt" -> 11:30 PM
  //          "yyyy-MM-dd HH:mm:ss"

  void Update()
  {
    if (timeText != null)
    {
      timeText.text = DateTime.Now.ToString(timeFormat);
    }
  }
}
