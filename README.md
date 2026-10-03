# ☀️ SolarPulse

> **Intelligent Solar Inverter Telemetry, Grid Availability & Energy Analytics Console**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![EF Core](https://img.shields.io/badge/Entity%20Framework%20Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 📖 Overview

**SolarPulse** is an automated energy monitoring system. It connects directly to solar inverter cloud platforms (such as Siseli / Deye / Solarman), pulls telemetry data on a schedule, stores the readings in a SQL Server database, and serves a live interactive dashboard.

Whether you want to track how much solar power your panels generate, monitor power cuts and grid uptime, or analyze your monthly electricity consumption, SolarPulse provides instant insights.

---

## ✨ Key Features

- **🔄 Automatic Telemetry Sync**: Background worker regularly fetches live power, voltage, current, load, battery, and solar panel metrics.
- **⚡ Grid Availability & Outage Tracking**: Calculates exact grid uptime hours, outage hours, and reliability percentage for any week or month.
- **📊 Interactive Web Dashboard**:
  - Live weekly availability gauge (e.g. 84.5% grid uptime).
  - 24-hour diurnal profile bar charts (Grid vs. Solar vs. Total consumption).
  - Monthly metrics comparison with solar vs. grid share percentages.
  - CSV export for raw telemetry analysis.
  - Dark mode and light mode toggle.
- **🚀 REST API with Swagger**: Clean API endpoints documented with OpenAPI / Swagger UI.
- **🛡️ Enterprise Resilience**: Integrated HTTP resilience, retry policies, and automatic Entity Framework Core migrations on startup.

---

## 🛠️ Tech Stack

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Backend** | .NET 10 / ASP.NET Core | High-performance Web API |
| **Database** | Microsoft SQL Server / EF Core 10 | Relational storage & migrations |
| **Frontend** | React 18 + Modern CSS | Single-file reactive dashboard |
| **API Docs** | Swagger / OpenAPI 3.0 | Interactive API testing interface |
| **Resilience** | `Microsoft.Extensions.Http.Resilience` | Automatic network retries and timeout protection |

---

## 🚀 Quick Start (In Simple Steps)

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or newer
- [SQL Server](https://www.microsoft.com/sql-server/) (LocalDB, Express, or Cloud SQL)

### 2. Clone the Repository
```bash
git clone https://github.com/ahmadTaieb/SolarPulse.git
cd SolarPulse
```

### 3. Configure Your Credentials
To protect your email, passwords, and database connection strings, never put real secrets into `appsettings.json`.

Instead, run these commands in your terminal:
```bash
# Initialize User Secrets for the Solar project
dotnet user-secrets init --project Solar

# Set your SQL Server connection string
dotnet user-secrets set "ConnectionStrings:SolarDatabase" "Server=YOUR_SERVER;Database=SolarDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;" --project Solar

# Set your Solar Cloud login credentials
dotnet user-secrets set "SolarPlatform:Email" "your_email@example.com" --project Solar
dotnet user-secrets set "SolarPlatform:Password" "your_password" --project Solar
dotnet user-secrets set "SolarPlatform:StationId" "YOUR_STATION_ID" --project Solar
dotnet user-secrets set "SolarPlatform:DeviceId" "YOUR_DEVICE_ID" --project Solar
```

> 💡 **Why User Secrets?** 
> `dotnet user-secrets` saves your passwords on your personal computer outside this folder, so they can **never** be accidentally uploaded to GitHub!

### 4. Run the Application
```bash
dotnet run --project Solar
```

The database tables will be created automatically on the first launch.

### 5. Open in Browser
- **Live Web Dashboard**: [http://localhost:5000](http://localhost:5000)
- **Interactive Swagger API Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

## 🔌 API Endpoints Summary

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/solar/sync-now` | Manually triggers immediate sync with the cloud platform |
| `GET` | `/api/solar/telemetry/latest` | Gets the latest instantaneous inverter telemetry |
| `GET` | `/api/solar/telemetry/history` | Gets telemetry logs within a date range |
| `GET` | `/api/solar/metrics/daily` | Gets daily aggregated energy & grid uptime metrics |
| `GET` | `/api/solar/metrics/hourly-grid`| Gets 24-hour diurnal load & generation profiles |
| `GET` | `/api/solar/metrics/monthly` | Gets monthly energy totals and uptime percentages |
| `GET` | `/api/solar/status` | Ingestion service status, device info, and database counts |

---

## 🔒 Security Best Practices for Publishing to GitHub

When publishing this project to a public repository:
1. **Never commit passwords**: Ensure `Solar/appsettings.json` only contains placeholder values.
2. **Use `.gitignore`**: The included `.gitignore` protects your compiled files (`bin/`, `obj/`, `publish/`) and local configurations.
3. **Change Exposed Passwords**: If your account or database password was previously saved in plain text, change it on your provider immediately.

---

## 🧪 Running Tests

Run the automated unit and integration tests:
```bash
dotnet test
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
