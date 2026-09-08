# Deployment Guide — Rice Store (live on the internet)

This gets the whole app (frontend + 7 services + SQL Server + RabbitMQ + Redis)
running on a single Linux server with Docker, reachable by anyone.

## What you need

1. A **cloud Linux server (VM)** with at least **4 GB RAM** (SQL Server + all
   services need it). Good options:
   - DigitalOcean Droplet (~$24/mo, 4 GB) — simplest
   - Azure VM, AWS EC2, Hetzner (cheapest), Linode
2. Optional: a **domain name** (~$10/yr) — otherwise you use the server's IP.
3. Your **Brevo SMTP credentials** (already have these).

## Overview

```
Browser ──HTTP──► [ frontend :80 (nginx) ]
                          │  calls the gateway URL you configure
                          ▼
                  [ gateway :8000 ] ──► identity / catalog / cart /
                                         inventory / order / payment
                                              │
                             SQL Server · RabbitMQ · Redis (all in Docker)
```

Everything runs via `docker-compose.prod.yml`.

---

## Step 1 — Create the server

Create a Ubuntu 22.04 VM (4 GB RAM). Note its **public IP** (e.g. `203.0.113.10`).
SSH in:

```bash
ssh root@203.0.113.10
```

## Step 2 — Install Docker on the server

```bash
curl -fsSL https://get.docker.com | sh
docker --version
```

## Step 3 — Get the project onto the server

Either push your project to GitHub and clone it, or copy it up with scp:

```bash
# from your Windows machine (PowerShell), zip excluded of junk first, or:
scp -r "C:\Users\nyaldanda\OneDrive - Infor\Desktop\rice-store" root@203.0.113.10:/opt/rice-store
```

Then on the server:

```bash
cd /opt/rice-store
```

## Step 4 — Create the .env file with your secrets

```bash
cp .env.example .env
nano .env
```

Fill in:

```
DB_PASSWORD=<a strong password>
JWT_KEY=<a long random string, 32+ chars>
EMAIL_SMTP_LOGIN=b7bc57001@smtp-brevo.com
EMAIL_SMTP_KEY=<your xsmtpsib- key>
EMAIL_FROM=yaldandanarsireddy@gmail.com
FRONTEND_GATEWAY_URL=http://203.0.113.10:8000      # <-- your server IP
```

> `FRONTEND_GATEWAY_URL` is how the browser reaches the API. Use your server's
> public IP (or domain). It must be reachable from outside, so use the public
> IP, not localhost.

## Step 5 — Open the firewall ports

Allow HTTP (80) for the site and 8000 for the gateway:

```bash
ufw allow 80
ufw allow 8000
ufw allow 22      # keep SSH open
ufw enable
```

Also open these ports in your cloud provider's firewall/security group.

## Step 6 — Build and start everything

```bash
docker compose -f docker-compose.prod.yml --env-file .env up --build -d
```

First build takes several minutes. Check status:

```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f gateway
```

## Step 7 — Authorize the server's IP in Brevo

Email sends from the **server's** IP now, not your home PC. In Brevo →
**SMTP & API → SMTP → Authorized IPs**, add the server's public IP (or disable
the IP restriction). Otherwise you'll get `525 Unauthorized IP address`.

## Step 8 — Visit the site

Open in any browser:

```
http://203.0.113.10
```

The store loads, products come from the live database, login/orders/email all
work. Share that URL (or your domain) with anyone.

---

## Using a domain name (nicer than an IP)

1. Buy a domain (Namecheap, GoDaddy, Cloudflare).
2. Add an **A record** pointing to your server IP:
   - `ricestore.com`      → `203.0.113.10`  (frontend)
   - `api.ricestore.com`  → `203.0.113.10`  (gateway)
3. Set `FRONTEND_GATEWAY_URL=http://api.ricestore.com:8000` in `.env` and
   re-run step 6.

## Adding HTTPS (recommended for a real site)

Put a reverse proxy with automatic TLS in front. The easiest is **Caddy** —
it gets free Let's Encrypt certificates automatically. Ask and I can add a
Caddy service to the compose file that terminates HTTPS for both the frontend
and the gateway.

---

## Updating the app later

```bash
cd /opt/rice-store
git pull                      # or re-copy the files
docker compose -f docker-compose.prod.yml --env-file .env up --build -d
```

## Stopping / restarting

```bash
docker compose -f docker-compose.prod.yml down        # stop (keeps data)
docker compose -f docker-compose.prod.yml up -d        # start again
```

Database data persists in the `sqldata` Docker volume across restarts.

---

## Costs (rough)

- Server (4 GB VM): ~$20–24/month
- Domain: ~$10/year
- Brevo email: free tier (300/day)

## Security checklist before going truly public

- [ ] Strong `DB_PASSWORD` and `JWT_KEY` in `.env` (never commit `.env`)
- [ ] Add HTTPS (Caddy/Let's Encrypt)
- [ ] Add JWT role checks on admin endpoints (currently client-side only)
- [ ] Consider not exposing 8000 publicly and routing the API through the same
      domain via the reverse proxy
- [ ] Replace the simulated payment flow with a real gateway (Razorpay) before
      taking real money
