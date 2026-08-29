# FMS — Fleet Management System

> **RGBSI Multi-Tenant SaaS Platform** serving 8 business sectors across multiple countries.

![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Angular](https://img.shields.io/badge/Angular-17-red)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- Docker & Docker Compose
- Git

### 1. Clone & Start Infrastructure
```bash
git clone https://github.com/your-org/fms.git
cd fms
docker-compose up -d
```

This starts:
- **PostgreSQL 16** (port 5432) — with sample data
- **MongoDB 7** (port 27017) — for telemetry
- **Redis 7** (port 6379) — for caching
- **RabbitMQ 3.13** (port 5672) — for message queue
- **Mosquitto MQTT** (port 1883) — for device ingestion

### 2. Start Backend API
```bash
dotnet run --project src/API
```
API runs at: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

### 3. Start Frontend
```bash
npm install
ng serve --project shell
```
Shell app runs at: `http://localhost:4200`

### 4. Login with Sample Data
| Tenant | Email | Password | Subdomain |
|--------|-------|----------|-----------|
| Acme Logistics Corp | admin@acme-logistics.com | Admin@123 | acme-logistics |
| SafeRide Taxi Services | admin@saferide-taxi.com | Admin@123 | saferide-taxi |
| Gulf Mining Group | admin@gulf-mining.com | Admin@123 | gulf-mining |

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│                ANGULAR SHELL APP                     │
│   Module Federation → 8 Sector Remotes (lazy load)  │
│   Dynamic Tables • JSON Forms • White-Label Theming │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/SignalR
┌──────────────────────┴──────────────────────────────┐
│                .NET 8 API (CQRS + MediatR)           │
│   Clean Architecture: Domain → Application → Infra   │
│   Tenant Resolution • RLS • Feature Gates            │
└──────────────────────┬──────────────────────────────┘
                       │
    ┌──────────────────┼──────────────────┐
    │                  │                  │
┌───┴────┐       ┌─────┴─────┐      ┌────┴────┐
│PostgreSQL│      │  MongoDB  │      │  Redis  │
│  (RLS)  │      │(Telemetry)│      │ (Cache) │
└─────────┘      └───────────┘      └─────────┘
```

## 📦 Project Structure

```
fms/
├── CLAUDE.md                    # AI context file
├── FMS.sln                      # .NET Solution
├── docker-compose.yml           # Local dev stack
├── src/
│   ├── Domain/                  # Entities, Interfaces
│   ├── Application/             # CQRS Commands/Queries, DTOs
│   ├── Infrastructure/          # EF Core, MongoDB, JWT, Repos
│   └── API/                     # Controllers, Middleware, Seed
├── tests/
│   ├── Unit/                    # xUnit + NSubstitute
│   └── Integration/             # WebApplicationFactory
├── agent_docs/                  # Architecture docs
├── infra/                       # Terraform IaC
├── scripts/                     # DB init scripts
├── config/                      # Mosquitto config
└── .github/workflows/           # CI/CD pipelines
```

## 🌐 UAT Environment URLs

| Service | URL | Status |
|---------|-----|--------|
| API | `https://fms-api-uat.onrender.com` | 🟢 |
| Web App | `https://fms-web-uat.vercel.app` | 🟢 |
| Admin Portal | `https://fms-admin-uat.vercel.app` | 🟢 |
| MQTT | `mqtt://fms-mqtt-uat.onrender.com:1883` | 🟢 |

## 📋 8 Business Sectors

| # | Sector | Status |
|---|--------|--------|
| 1 | 🚛 Logistics | ✅ Phase 1 |
| 2 | 🚕 Taxis | ✅ Phase 1 |
| 3 | 🎒 School Buses | ✅ Phase 1 |
| 4 | 🚑 Ambulances | ✅ Phase 1 |
| 5 | 🚌 Public Transport | 📋 Phase 2 |
| 6 | ⛏️ Mining | 📋 Phase 2 |
| 7 | 🚆 Railways | 📋 Phase 2 |
| 8 | 🚔 Law Enforcement | 📋 Phase 2 |

## 🔧 Commands

```bash
# Start everything
docker-compose up -d
dotnet run --project src/API
ng serve --project shell

# Run tests
dotnet test
ng test --watch=false

# Build for production
dotnet publish -c Release -o ./publish
ng build --configuration=production

# Database migrations
dotnet ef migrations add {Name} --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

## 📄 License

MIT License — RGBSI Fleet Management
