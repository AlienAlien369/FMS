# FMS Tenant Onboarding Flow

## Self-Service Signup
1. **Landing Page:** "Start Free Trial" CTA
2. **Step 1 — Company Profile:**
   - Company name
   - Subdomain (auto-check availability)
   - Admin email
   - Country (determines data residency)
3. **Step 2 — Plan & Sectors:**
   - Select plan (Basic/Pro/Enterprise)
   - Select sectors (1+ of 8)
   - Estimated device count
4. **Step 3 — Confirmation:**
   - Review selections
   - Accept terms
   - Submit

## Auto-Provisioning (Async, < 2 minutes)
1. Create tenant record in `tenants` table
2. Provision PostgreSQL schema (shared DB) or database (enterprise)
3. Create MongoDB collections with shard key
4. Create Cloudflare R2 bucket folder
5. Seed default roles and permissions
6. Create first admin user (send welcome email with temp password)
7. Initialize MQTT topic namespace
8. Send branded welcome email via SendGrid

## Trial Management
- 14-day free trial
- Usage limits enforced via API Gateway rate limiting
- Daily usage reports to admin email
- Upgrade prompt at day 10
- Auto-suspend at day 15 if no payment

## UAT Onboarding URLs
- **Signup:** `https://fms-web-uat.vercel.app/signup`
- **Admin Portal:** `https://fms-admin-uat.vercel.app`
