
# SkySync_Transit

## Description

SkySync_Transit is a hybrid system combining **physical devices** and a **digital twin**, designed specifically to assist **UCL students** in planning their bus trips after classes. The system integrates **bus schedules**, **weather**, **walking times**, and **CE course timetable data** to provide real-time recommendations and visual feedback.

**Motivation:**  
UCL students often need to quickly decide which bus to catch after class based on their course schedule and weather conditions. SkySync_Transit was created to make commuting more intuitive, reducing stress and helping students make informed decisions.

**Why this project:**  
Instead of relying solely on apps or static timetables, SkySync_Transit combines physical interaction, course timetable data, and a digital dashboard, allowing users to instantly see bus recommendations, estimated walking times, and weather conditions tailored to their class schedule.

**Problem it solves:**  
Helps UCL students efficiently plan which bus to take after class, estimate walking times under different weather conditions, and visually understand the urgency of catching the next bus.

**What we learned:**  
- Integration of hardware (sensors, servo, LED, LCD) with APIs and MQTT data streams  
- Visualizing time-sensitive information using both physical pointers and a digital gauge dashboard  
- Combining CE timetable data with real-world commuting decisions  
- Building a digital twin that accurately mirrors physical device behavior  

---

## Table of Contents

- [Overview](#overview)
- [Physical Device](#physical-device)
- [Digital Twin](#digital-twin)
- [Gauge Dashboard Design](#gauge-dashboard-design)
- [Software & Data Interfaces](#software--data-interfaces)
- [Connection](#connection)
- [Usage](#usage)
- [Credits](#credits)
- [License](#license)

---

## Overview

SkySync_Transit allows UCL students to select a bus route using a rotary knob. Based on the selected bus, current weather, walking distance, and class schedule, the system calculates estimated travel time, recommends the next best bus, and provides visual feedback through an LCD display, a physical pointer, and LED strip. All data is synced via MQTT and APIs.

---

## Physical Device

The physical device provides a tangible, real-time representation of weather conditions, walking time, and bus urgency. It includes:

### Hardware Components

- **ESP8266 Microcontroller**  
  - Core controller for all hardware components  
  - Connects to Wi-Fi to access APIs and MQTT topics  
  - Handles sensor input, servo output, LED control, and data processing  

- **Rotary Angle Sensor v1.2**  
  - Used to select the bus number (108 or 339)  
  - Automatically detected on startup, triggering timetable API requests  

- **MG90S Servo Motor**  
  - Drives the analog-style pointer  
  - Pointer position corresponds to weather condition, temperature segment, and calculated walking time  

- **LCD Display (Grove-LCD RGB Backlight)**  
  - Shows estimated walking time based on weather + temperature  
  - Displays bus timetable returned from the API  
  - Highlights the recommended bus based on UCL CE class schedule  

- **LED Strip (BTF 5V 60LED/m)**  
  - Uses color gradients to visualize urgency of catching the next bus  
  - Updates dynamically based on timetable and walking-time calculations  

- **3D-Printed Structure**  
  - Two-print materials used: opaque + translucent  
  - Represents walking route from One Pool Street to the destination  
  - Houses the gauge instrument panel (print + lamination)

---

## Digital Twin

The digital twin mirrors the behavior of the physical device and provides **more detailed data**:

- Digital gauge dashboard showing the same segments, zones, and walking-time pointer  
- Displays more bus options than the physical device  
- Shows detailed weather information and suggestions for travel  
- Allows users to **scan different images** to switch between bus lines  
- Provides a richer monitoring interface for debugging and live demonstration  

---

## Gauge Dashboard Design

The **gauge dashboard** is the visual system used both in the physical device and digital twin.

### Zones & Segments

- Weather zones (left → right): **Sunny → Cloudy → Rainy → Snowy**  
- Each zone has **6 temperature segments**:

| Segment | Temperature (°C) |
|---------|------------------|
| 0       | temp ≤ 0         |
| 1       | 0 < temp ≤ 5     |
| 2       | 5 < temp ≤ 10    |
| 3       | 10 < temp ≤ 20   |
| 4       | 20 < temp ≤ 25   |
| 5       | temp > 25        |

- Total walking time range: **4–8 minutes**  
- Each segment = **10 seconds**  
- Example: Rainy + 8.8°C → zone 3, segment 3 → 380 sec → 6.33 minutes  

Both APIs and MQTT provide pre-calculated results:
- Weather  
- Temperature  
- Walking time (seconds + minutes)  
- Zone index  
- Segment index  
- Final pointer target

---

## Software & Data Interfaces

### Bus Timetable API
- **Endpoints**:  
  - `http://10.129.111.26:3000/api/timetable/108`  
  - `http://10.129.111.26:3000/api/timetable/339`  
- Returns full-day schedules for selected bus  
- Includes times for **Aquatic Center** and **Stratford Station**  
- Provides **recommended next bus** based on CE timetable (class ends at 13:00)  
- **MQTT topics** available for live streaming (use lowercase *qingshan*)

### Weather & Walking Time API
- `http://10.129.111.26:3000/api/current-walk`  
- Returns temperature, weather, pointer zone/segment, and walking time  

### CE Course Timetable Integration
- Ensures bus recommendation fits UCL students’ real class schedules  
- The system chooses the next bus *after* class end time (1 PM)

---

## Connection

1. Connect all hardware components.  
2. Connect to the UCL classroom network (required for API access).  
3. Install libraries for sensors, servo, LED strip, LCD display, and MQTT.  
4. Clone the repository to the ESP8266.  
5. Configure MQTT topics (`qingshan/...`).  
6. Power the device to begin automatic detection and data fetching.  

---

## Usage

1. Power on the system.  
2. Select bus route via the rotary knob.  
3. Device fetches timetable via API or MQTT.  
4. Weather and walking data are retrieved from the walking API.  
5. System calculates estimated walking time and bus recommendation.  
6. Data is displayed via LCD, pointer gauge, and LED urgency indicator.  
7. Digital twin shows additional details for debugging or demonstration.
8. 
---

## System Flow


Rotary Knob -> Detect Bus Number -> Fetch Bus Timetable (API/MQTT) 
             -> Fetch Weather & Walking Time -> Calculate Estimated Travel Time 
             -> Physical Gauge + LCD + LED Output 
             -> Digital Twin Dashboard Rendering


---

## Credits

* Project Team Members:

  * [Qingshan Luo](https://github.com/malus-eng)
  * [Xinyi Zhang](https://github.com/CynthiaZHANGovo)
  * [Yidan Gao](https://github.com/gydgzh)
  * [Zihang He](https://github.com/xms12138)

---

## License

This project is licensed under the [MIT License](https://choosealicense.com/licenses/mit/).



---
