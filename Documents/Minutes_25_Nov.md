# Meeting Minutes  – Transport
**Date:** 25-Nov-2025  

## 1. Theme & Overall Idea

- We decided to go with the **Transport** theme.
- After chatting with **Andy** and **Valerio**, the plan is:

  - Use a **timetable API** (bus/train/car) + **weather API**.  
  - Consider **walking time** from **UCL East campus → tube station**.  
  - Combine with the **course timetable** so the system:
    - Knows **when class ends**.
    - Suggests the **best departure** and **a couple of alternatives** before/after.
  - “Highlight” idea:  
    - Use **weather** (rain, wind, etc.) to adjust how long it takes to walk from campus to the station.  
    - Visualise this nicely (both physically and digitally).

---

## 2. Physical Device

The physical device will **visualise vehicle timetables**. Planned components:

- **Vehicle model box**
  - A physical model representing the bus

- **Gear motor + progress bar**
  - A mechanical progress bar driven by a motor, showing journey progress.

- **Simple LED display**
  - Displays key info like:
    - Weather info 
    - Route / line chose
    - Minutes left
    - ...

- **Route switch button**
  - Press to switch between routes (different lines).

---

## 3. Digital Twin

We’ll have **two main 2D screens**:

1. **Weather screen**
   - Shows:
     - Temperature  
     - Rainfall level  
     - UV index  
     - Wind strength
     - ...
   - Also gives simple suggestions like:
     - “Bring an umbrella.”  
     - “Wear a coat.”  
     - “Windy – leave a bit earlier.”

2. **Timetable and journey panel**
   - Displays:
     - Class end time  
     - Relevant transport line(s)  
     - Estimated time needed to walk from UCL East campus to the tube station (adjusted by weather)  
     - The **best** departure option and **alternative options** (earlier and later services)  
   - If time allows, this panel may be enhanced with the **MapBox** to visualise the route and position.

---

## 4. Work Distribution For This Week

- **Yidan Gao**  
  - Build a basic **Unity** setup for the digital twin (two panels: weather + timetable).

- **Qingshan Luo**  
  - Try using **MKR1010 WiFi** to connect to the **timetable API** and **weather API**.

- **Zihang He & Xinyi Zhang**  
  - Build the **gear motor–driven progress bar** for the physical device.

---
