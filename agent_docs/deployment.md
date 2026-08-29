# FMS Deployment Guide

## Environments

### Development (Local)
```bash
docker-compose up -d
# Services: PostgreSQL 16, MongoDB 7, Redis 7, RabbitMQ 3.13, Mosquitto 2
```

### UAT (Free Tier)
| Service | Provider | URL/Details |
|---------|----------|-------------|
| API | Render | `https://fms-api-uat.onrender.com` |
| Angular Shell | Vercel | `https://fms-web-uat.vercel.app` |
| Platform Admin | Vercel | `https://fms-admin-uat.vercel.app` |
| PostgreSQL | Neon | UAT branch |
| MongoDB | Atlas M0 | Free (512MB) |
| Redis | Upstash | Free (10K/day) |
| RabbitMQ | CloudAMQP | Free (1M msgs) |
| MQTT | Mosquitto on Render | `mqtt://fms-mqtt-uat.onrender.com:1883` |
| Storage | Cloudflare R2 | Free (1GB) |

### Production (Scale as Needed)
| Service | Provider | Cost Est. |
|---------|----------|-----------|
| API | Render Pro | $20-50/mo |
| PostgreSQL | Neon Scale | $19-50/mo |
| MongoDB | Atlas M10 | $57/mo |
| MQTT | EMQX Cloud | $0-20/mo |
| CDN | Cloudflare Pro | $20/mo |

## CI/CD Pipeline (GitHub Actions)
```yaml
name: FMS Deploy
on:
  push:
    branches: [main, staging]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - name: Setup Node
        uses: actions/setup-node@v4
        with: { node-version: '20' }
      - name: Restore & Test .NET
        run: dotnet test --verbosity normal
      - name: Build Angular
        run: |
          npm ci
          ng build --configuration=production

  deploy:
    needs: test
    if: github.ref == 'refs/heads/staging'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy API to Render
        run: curl -X POST ${{ secrets.RENDER_DEPLOY_HOOK }}
      - name: Deploy Frontend to Vercel
        run: npx vercel --prod --token=${{ secrets.VERCEL_TOKEN }}
```

## Secrets Required (GitHub)
- `NEON_DATABASE_URL` — PostgreSQL connection string
- `MONGODB_CONNECTION_STRING` — MongoDB Atlas connection
- `REDIS_CONNECTION_STRING` — Upstash Redis URL
- `RABBITMQ_CONNECTION_STRING` — CloudAMQP URL
- `JWT_SECRET_KEY` — JWT signing key (min 32 chars)
- `SENDGRID_API_KEY` — Email service
- `RENDER_DEPLOY_HOOK` — Render auto-deploy
- `VERCEL_TOKEN` — Vercel deployment token
