# SkySync_Transit

## Work Division Agenda

**Course:** CASA0019: Sensor Data Visualisation (25/26)

**Total length:** \~2000 words (±10%)

---

## 1. Front Page & Administrative Info（≈150 words）

- Project Title: _SkySync_Transit_

- Course name & academic year

- Group members（姓名 + GitHub username）

- GitHub repository link（可访问）

- 简短 project tagline（1–2 句）

---

## 2. Project Rationale & Context（≈300 words）

- 为什么选择“UCL 学生通勤”作为主题

- _bus + weather + timetable_？

- 体现 **Sensor Data Visualisation**

- 与 CASA0019 课程内容（physical computing / data devices / visualisation

---

- 真实场景（One Pool Street → Stratford）

- 时间敏感性（下课 → 赶车）

- 信息压力 vs 直观可视化的价值

---

## 3. System Overview & Design Concept（≈250 words）

- Physical device + Digital twin 的设计逻辑

- API / MQTT / Sensors

- Digital Twin（debug / scalability / demo）

- System flow diagram

---

## 4. Physical Device Design & Implementation（≈400 words）

### 4.1 Hardware Design

- ESP8266&#x20;

- Rotary sensor ( input

- Servo + LED + LCD ( output

- 3D 打印结构（route / gauge）

### 4.2 Data Visualisation Logic（Physical）

- 指针 \= walking time & weather

- LED \= urgency / time pressure

- LCD \= numerical & textual 补充

---

## 5. Digital Twin & Dashboard Design（≈300 words）

- Digital twin 与 physical device 同步

- Gauge zones & segments&#x20;

- “更详细信息”

- 图像识别 ( bus line switching ）

---

## 6. Software, Data & Technologies（≈300 words）

### The data transmission portion of this project primarily relies on the MQTT protocol to transmit signals to the assembled electronic devices and on the API to transmit data to the digital twin system. The project mainly established three MQTT topics for devices to subscribe to: the timetables for bus routes 339 and 108, and the current London temperature and estimated walking time to the station. The API also provides the same three types of data. Using these three datasets, the digital twin and devices can effectively acquire data and display it clearly and efficiently.

More precisely, the data in the project mainly comes from publicly available data on authoritative websites. The timetable information for bus routes 339 and 108 is entirely sourced from the TFL website. Additionally, the London weather and temperature information is obtained from the Open-Meteo website, ensuring overall accuracy and reliability (Paudyal, Shakya and Upadhayaya, 2025). The data printed in this project is formatted as shown in the image below.

### 6.3 Tools & Platforms

- Arduino / ESP8266

- Unity / dashboard tools

- GitHub as documentation & reproducibility platform

---

## 7. Contributions（≈200 words）

-        *   Hardware & electronics

         *   Software & API integration

         *   Digital twin & UI

         *   Fabrication & physical design

         *   Documentation & GitHub management

  ***

## 8. Evaluation: What Worked & What Didn’t（≈200 words）

###

---

## 9. Future Improvements & Modifications（≈200 words）

- 更多 bus routes

- User-specific timetable

- Mobile companion app

- Improved enclosure / durability

- Broader deployment beyond UCL

---

## 10. Repository Structure & Reproducibility（≈150 words）

- GitHub repo 结构说明

- Code / Unity / STL / diagrams

- How to reproduce the project

- Reference & citation strategy

---

## 11. Conclusion（≈100 words）

- 项目价值总结

- 与 CASA0019 learning outcomes 对应

- Physical data visualisation 的意义

