# 🚗 Smart Parking System 3D — Autonomous Multi-Level Facility Simulation

[![Unity 6.3 LTS](https://img.shields.io/badge/Unity-6000.3.27f1%20LTS-black?logo=unity)](https://unity.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2064--bit-blue?logo=windows)](https://github.com/vikasah312-dev/Smart-Parking-System-3D-v1.0.0/releases/tag/v1.0.0)
[![Pipeline](https://img.shields.io/badge/Render%20Pipeline-Universal%20RP-lightgrey)](https://unity.com/srp/universal-render-pipeline)
[![Release](https://img.shields.io/badge/Release-v1.0.0-brightgreen?logo=github)](https://github.com/vikasah312-dev/Smart-Parking-System-3D-v1.0.0/releases/tag/v1.0.0)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

An interactive, high-fidelity 3D Smart Parking Simulation built with **Unity 6.3 LTS** and the **Universal Render Pipeline (URP)**. Engineered for academic, technical, and college final-year engineering demonstrations. Features realistic vehicle dynamics, automated barrier gates, 6-sensor ultrasonic parking radar, an automatic rear-view backup camera, multi-level facility telemetry across 84 parking bays, real-time minimap radar, and timed parking challenge missions with dynamic scoring.

---

## 📥 Direct Windows Download

| Release Asset | Version | Platform | Direct Download Link |
| :--- | :--- | :--- | :--- |
| **`SmartParking3D_Final_Windows.zip`** | `v1.0.0` | Windows 10 / 11 (64-bit) | [**Download SmartParking3D_Final_Windows.zip**](https://github.com/vikasah312-dev/Smart-Parking-System-3D-v1.0.0/releases/download/v1.0.0/SmartParking3D_Final_Windows.zip) |

---

## 🌟 Key Features

### 1. Realistic Environment & Visuals (URP)
- **High-Fidelity PBR Materials:** Physically-accurate tarmac asphalt with specular road sheen, glossy epoxy garage bay floors, cast architectural concrete pillars, and safety striping.
- **Automotive Metallic Clearcoat:** 13 car body paint variants featuring high-reflectance metallic clearcoat, tinted glass, and matte rubber tires.
- **Lighting & Atmospheric Ambiance:** Soft directional daylight balanced with non-shadowing ambient fill and 4 real-time localized Box Reflection Probes (L0, L1, L2, Exterior) reflecting ceiling light fixtures and structural columns.
- **Cinematic Post-Processing:** ACES filmic tonemapping, subtle bloom on LED beacons and wayfinding signage, color grading, and subtle vignette.
- **Vehicle Illumination:** Toggled xenon/LED headlights (`[L]`) with physical spotlights, running lights, dynamic brake lights, and bright white reverse lights.

### 2. Intelligent Parking & Sensor Suite
- **Ultrasonic Parking Radar (6 Sensors):** Triple-sensor arrays on front and rear bumpers detecting obstacles up to 3.5m away with color-coded warning bars (Green > 2.0m, Yellow 1.0–2.0m, Red < 1.0m, and critical STOP warning).
- **Procedural Sonar Audio Alert:** Real-time piezo transducer warning beeps with pulse frequency accelerating as the vehicle approaches any obstacle.
- **Automatic Rear-View Backup Camera:** Wide-angle rear bumper camera automatically feeds to an in-cockpit picture-in-picture display with dynamic distance trajectory guidelines whenever Reverse (`[R]`) gear is engaged.
- **Precision Validation Logic:** Confirms parking only when vehicle is inside bay boundaries and stationary ($\le 0.85\text{ km/h}$) for $\ge 1.0\text{ s}$.

### 3. Multi-Floor Telemetry & Facility Control
- **Live Occupancy Tracking:** Monitors all 84 slots across 3 levels (Ground L0, Mid-Deck L1, Rooftop L2).
- **Dynamic LED Indicators:** 3D beacon lights directly above each bay (Green = Vacant, Red = Occupied, Cyan = Recommended Guidance Target).
- **Automated Entrance & Exit Gates:** Proximity-triggered barrier arm rotations with safety sensor clearance.
- **Live Navigation Minimap (`[M]`):** Real-time top-down orthographic radar displaying facility boundaries, vehicle heading, and parking bay occupancy states.

### 4. Competitive Parking Missions & UI Suite
- **Mission 1:** Ground Floor Bay (Target: Bay L0-A02, Par Time: 35s)
- **Mission 2:** Precision Reverse Park (Target: Bay L0-A04, Par Time: 48s)
- **Mission 3:** Level 1 Ramp Ascent (Target: Bay L1-A01, Par Time: 70s)
- **Live Mission HUD & Scoring:** Tracks countdown timer, collision bump penalties, live score, and awards 1 to 3 stars with letter grades (S, A, B, C).
- **Responsive Theme:** Translucent dark glass UI scaling seamlessly across $1280 \times 720$ and $1920 \times 1080$ with zero element overlap.

---

## 📸 Screenshots & Visual Walkthrough

> *Simulation captures showcase the multi-level facility, in-game telemetry, sensor HUD, and camera modes.*

```
+-----------------------------------------------------------------------------------+
|  [P] Ground Floor Entrance & Automatic Gates                                      |
|  Proximity-activated barrier arm raises smoothly as the vehicle approaches.       |
|                                                                                   |
|  [P] In-Cockpit Telemetry HUD & Speedometer                                       |
|  Real-time digital speedometer, gear indicator (D/N/R), and live occupancy stats. |
|                                                                                   |
|  [P] Rear-View Backup Camera & Radar Proximity Bar                                |
|  Engaging Reverse gear activates wide-angle camera with distance trajectory lines.|
|                                                                                   |
|  [P] Parking Challenge System                                                     |
|  Multi-stage parking missions with real-time countdown, score, and star ratings.  |
+-----------------------------------------------------------------------------------+
```

---

## 🎮 Controls & Shortcuts

| Action | Primary Key | Alternate | Description |
| :--- | :--- | :--- | :--- |
| **Accelerate / Drive (D)** | `W` | `Up Arrow` | Forward throttle with progressive acceleration |
| **Reverse (R) / Decelerate** | `S` | `Down Arrow` | Decelerate, brake, or reverse vehicle |
| **Steer Front Wheels** | `A` / `D` | `Left / Right` | Smooth steering with visual wheel rotation |
| **Handbrake (P)** | `Spacebar` | — | Locks rear wheels and clamps low-speed creep |
| **Toggle Headlights** | `L` | — | Switches xenon spotlights and running lights |
| **Toggle Minimap Radar** | `M` | — | Cycles Compact / Expanded / Off navigation map |
| **Cycle Camera Views** | `C` | — | Cycles all 5 exterior and tracking camera angles |
| **Direct Camera Select** | `1` to `5` | Numpad `1` to `5` | Instantly jump to specific camera angle |
| **Pause / In-Game Menu** | `Escape` | — | Opens pause menu, settings, and mission selection |
| **Toggle Telemetry HUD** | `H` | — | Hides or reveals HUD overlay for clean screenshots |

---

## 📷 Camera View Modes

1. **Third-Person Chase (`[1]`):** Smooth follow camera with `Physics.SphereCast` collision dampening against garage columns and ceilings.
2. **Top-Down Overview (`[2]`):** Comprehensive overhead view of the full 3-level parking complex.
3. **Entrance Gate Cam (`[3]`):** Security camera monitoring vehicle approach and barrier animations.
4. **Bay Guidance Cam (`[4]`):** Direct view of the recommended vacant parking bay.
5. **Cinematic Exterior Orbit (`[5]`):** Continuous orbital fly-around showcasing the facility architecture.

---

## 💻 System Requirements

### Minimum Requirements
- **OS:** Windows 10 (64-bit) / Windows 11 (64-bit)
- **Processor:** Intel Core i3-6100 / AMD Ryzen 3 1200 or equivalent
- **Memory:** 4 GB RAM
- **Graphics:** DirectX 12 compatible GPU (Intel UHD 630 / NVIDIA GeForce GTX 750 / AMD Radeon HD 7850)
- **Storage:** 250 MB available hard drive space

### Recommended Requirements
- **OS:** Windows 10/11 (64-bit)
- **Processor:** Intel Core i5-8400 / AMD Ryzen 5 2600 or higher
- **Memory:** 8 GB RAM
- **Graphics:** NVIDIA GeForce GTX 1060 / AMD Radeon RX 580 or higher
- **DirectX:** Version 12

---

## 📦 Installation & Running

1. Download **[`SmartParking3D_Final_Windows.zip`](https://github.com/vikasah312-dev/Smart-Parking-System-3D-v1.0.0/releases/download/v1.0.0/SmartParking3D_Final_Windows.zip)**.
2. Extract the ZIP archive into any folder on your Windows computer.
3. Open the extracted folder `SmartParking3D/`.
4. Double-click **`SmartParking3D.exe`** to launch the simulation.
5. In the Main Menu, choose **Free Roam Simulation** or select a **Parking Challenge** mission.

---

## 🏗️ Technical Specifications

- **Engine:** Unity 6000.3.27f1 (Unity 6.3 LTS)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Target Platform:** Windows 64-bit Standalone (DirectX 12)
- **Audio Engine:** Real-time procedural DSP synthesis via `OnAudioFilterRead` (no external audio assets required)
- **Physics Engine:** Continuous collision detection, realistic tire lateral friction, aerodynamic downforce, anti-roll bars
- **Slot Capacity:** 84 Total Parking Bays across 3 Levels (L0 Ground, L1 Mid-Deck, L2 Rooftop)

---

## 📜 License & Acknowledgments

This project is released under the [MIT License](LICENSE). Built for academic, demonstration, and simulation research purposes.
