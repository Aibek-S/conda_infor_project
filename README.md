# Conda Info — School Information System

Desktop information system for schools: class management, activity submission and monitoring. **C# WinForms client** with a **Supabase** backend (Postgres + Edge Functions).

## Features

- **Auth** — login / registration with JWT (Supabase auth).
- **Classes** — class creation and management (`ClassesForm`, `CreateClassForm`).
- **Teacher dashboard** — teacher view of classes and activities.
- **Activity submission** — submit activities via Supabase Edge Function (`submit-activity`).
- **Student agent** — student-facing form.
- **Process monitoring** — `ProcessMonitorService` + `process_snapshot.py` snapshot script for activity tracking.
- **Logging** — structured logger service.

## Structure

```
conda_infor_project/
├── conda_infor_project/          # C# WinForms app (net10.0-windows)
│   ├── forms/                    # Login, Register, Classes, Dashboards
│   ├── services/                 # Auth, Activity, ProcessMonitor, Logger
│   ├── repository/               # Data access
│   ├── models/                   # User, Log, Class, Activity models
│   ├── db/                       # Database client
│   └── scripts/                  # Snapshot scripts (process_snapshot.py)
├── supabase/
│   ├── functions/
│   │   ├── create-class/         # Edge Function: class creation
│   │   └── submit-activity/      # Edge Function: activity submission
│   └── config.toml
├── tools/
├── INF_SCRIPT/
└── conda_infor_project.slnx     # Solution file
```

## Tech Stack

C# (.NET 10, WinForms) · Supabase (Postgres, Auth, Edge Functions in Deno/TypeScript) · Python (snapshot scripts)

## Setup

1. Open `conda_infor_project.slnx` in Visual Studio (or `dotnet build`).
2. Configure Supabase connection in `db/DataBase.cs` (URL + anon key).
3. Deploy Edge Functions to your Supabase project.
4. Run the desktop app.
