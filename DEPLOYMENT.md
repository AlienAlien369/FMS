# 🚀 FMS Deployment Guide

## ✅ What's Already Live

| Service | URL | Status |
|---------|-----|--------|
| **🌐 Web Frontend** | https://fms-web-lakshyas-projects-c97e54f6.vercel.app | ✅ LIVE |
| **📦 GitHub Repo** | https://github.com/AlienAlien369/FMS | ✅ PUSHED |
| **🐘 PostgreSQL** | Neon project `misty-sunset-31089713` | ✅ PROVISIONED |

## 🔧 Manual Render Setup (2 minutes)

The Render MCP doesn't support Docker services. Create it manually:

1. Go to **https://dashboard.render.com/web/new**
2. Click **"Build and deploy from a Git repository"** → Next
3. Connect your GitHub account if prompted
4. Select **AlienAlien369/FMS** → Next
5. Enter these settings:

| Setting | Value |
|---------|-------|
| **Name** | `fms-api` |
| **Runtime** | `Docker` |
| **Region** | Oregon (US West) |
| **Branch** | `main` |
| **Root Directory** | _(leave blank)_ |
| **Dockerfile Path** | `./Dockerfile` |
| **Docker Context** | `.` |
| **Plan** | Free |

6. Add these **Environment Variables**:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:5000` |
| `DATABASE_URL` | `postgresql://fms_owner:npg_pyYenLx1as7C@ep-soft-resonance-argxbo0h-pooler.c-4.us-west-2.aws.neon.tech/fms?sslmode=require` |
| `JWT_SECRET` | `fms-production-jwt-secret-2026-rgbsi-fleet!` |

7. Click **"Create Web Service"**

The build takes ~5-8 minutes for the first deploy.

## 🗄️ Neon Database

- **Project**: `misty-sunset-31089713`
- **Region**: US West 2 (Oregon)
- **Database**: `fms`
- **Connection**: `postgresql://fms_owner:npg_pyYenLx1as7C@ep-soft-resonance-argxbo0h-pooler.c-4.us-west-2.aws.neon.tech/fms?sslmode=require`

The .NET services will auto-create tables via EF Core migrations on first startup.

## 🌐 Vercel Frontend

- **URL**: https://fms-web-lakshyas-projects-c97e54f6.vercel.app
- **Project ID**: `prj_Wgxnetn6jmcslZfCsxeswER5H63R`
- **Auto-deploy**: Disabled (manual deploy only via Vercel CLI or dashboard)

## 📊 Architecture

```
┌─────────────────────────────────────────────────┐
│                  Vercel CDN                      │
│         https://fms-web.vercel.app               │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│              Render (Docker)                     │
│         https://fms-api.onrender.com             │
│  ┌─────────┬──────────┬──────────┬──────────┐  │
│  │   Auth  │  Entity  │ Telemetry│  Config  │  │
│  │ Service │ Service  │ Service  │ Service  │  │
│  └────┬────┴────┬─────┴────┬─────┴────┬─────┘  │
│       │         │          │          │         │
│  ┌────▼─────────▼──────────▼──────────▼──────┐  │
│  │     SharedKernel + MessageBus (MassTransit)│  │
│  └───────────────────────────────────────────┘  │
└─────────────────────┬───────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────┐
│           Neon PostgreSQL (Serverless)           │
│    misty-sunset-31089713 (US West 2)            │
│    Row-Level Security · Auto-scaling             │
└─────────────────────────────────────────────────┘
```

## 🔑 Demo Credentials

| Tenant | Email | Password |
|--------|-------|----------|
| Acme Logistics Corp | admin@acme-logistics.com | Admin@123 |
| SafeRide Taxi Services | admin@saferide-taxi.com | Admin@123 |
| Gulf Mining Group | admin@gulf-mining.com | Admin@123 |
