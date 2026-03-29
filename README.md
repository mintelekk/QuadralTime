# Quadral Time App

## Project Description

Quadral Time is a unique timekeeping application that reimagines the day as six 4-hour cycles, each with its own meridiem: AM, MM, DM, PM, EM, and NM. This system offers a fresh perspective on daily rhythms, making it easier to visualize and manage time in distinct segments. The app is designed for users who want to experiment with alternative time systems, improve focus, or simply enjoy a novel approach to scheduling and alarms.

The application features a visually engaging clock that displays the current time in Quadral format, a cycle counter, and a date display. Users can set recurring alarms using intuitive dropdown menus for hour, minute, and cycle, and alarms are shown in Quadral Time notation. The interface is styled with a modern dark mode and blue accent lighting for a polished Windows experience.

Quadral Time is ideal for productivity enthusiasts, alternative time system fans, and anyone interested in exploring new ways to structure their day. The app is built with reliability in mind, supporting persistent alarms, recurring schedules, and a responsive, user-friendly interface.

## How the Quadral Clock Works

- The day is divided into six cycles: AM (12am–4am), MM (4am–8am), DM (8am–12pm), PM (12pm–4pm), EM (4pm–8pm), and NM (8pm–12am).
- Each cycle contains four hours, and each hour is further divided into 15-minute intervals (00, 15, 30, 45).
- The clock face displays hours 1–4 for the current cycle, with a hand indicating the current time within the 4-hour revolution. The cycle counter at the top shows the current cycle and its range.
- Alarms are set and displayed in Quadral Time (e.g., 2:15 PM), and the app plays a sound when an alarm is triggered.

## Setup Instructions

### Prerequisites
- Windows 10 or later
- [.NET 10.0 SDK (Windows)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Dependencies
- No external NuGet packages required. Uses only .NET built-in libraries (WPF, System.Text.Json).

### Build and Run
1. Clone or download this repository to your local machine.
2. Open a terminal in the `QuadralTimeApp` directory.
3. Build the project:
   ```sh
   dotnet build
   ```
4. Run the app:
   ```sh
   dotnet run
   ```

### Usage Notes
- Alarms are saved automatically on exit and restored on startup.
- To delete an alarm, click the ✕ button next to it in the list.
- The app uses the system's Exclamation sound for alarms.

---

Enjoy exploring time in a new dimension with Quadral Time!