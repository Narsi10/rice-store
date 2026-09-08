# AWS Deployment — Rice Store (EC2 + Docker + HTTPS)

Step-by-step for AWS. Assumes you have an AWS account.

## ⚠️ Important: instance size

The AWS **free tier** (t2.micro / t3.micro) has only **1 GB RAM**. This app runs
SQL Server + RabbitMQ + Redis + 7 services + frontend and needs **~4 GB+**.
SQL Server alone wants ~2 GB. So:

- **Recommended:** `t3.large` (2 vCPU, 8 GB) — comfortable. NOT free (~$60/mo).
- **Minimum:** `t3.medium` (2 vCPU, 4 GB) — tight but works (~$30/mo).
- The free `t3.micro` will **not** run this stack.

You can stop the instance when not in use to save cost (you're billed per hour running).

---

## Step 1 — Launch an EC2 instance

1. AWS Console → **EC2** → **Launch instance**
2. Name: `rice-store`
3. AMI: **Ubuntu Server 22.04 LTS**
4. Instance type: **t3.medium** (or t3.large)
5. Key pair: **Create new**, download the `.pem` file (you SSH with it)
6. Storage: increase to **30 GB** (SQL Server + images need space)
7. Network settings → **Edit** → add inbound security group rules:
   - SSH (22) — your IP
   - HTTP (80) — anywhere (0.0.0.0/0)
   - HTTPS (443) — anywhere (0.0.0.0/0)
   - Custom TCP (8000) — anywhere (only if NOT using Caddy/domain)
8. **Launch instance**
9. Note the **Public IPv4 address** (e.g. `3.90.12.34`)

## Step 2 — Connect via SSH

From your Windows machine (PowerShell), in the folder with your `.pem` file:

```powershell
ssh -i rice-store.pem ubuntu@3.90.12.34
```

(If it complains about key permissions, that's a Windows quirk — you can also
use the EC2 "Connect" button in the console for a browser terminal.)

## Step 3 — Install Docker

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker ubuntu    # run docker without sudo
newgrp docker
docker --version
```

## Step 4 — Copy the project to the server

**Option 1 — via GitHub (recommended):** push your repo, then:
```bash
git clone https://github.com/YOURNAME/rice-store.git
cd rice-store
```

**Option 2 — via scp from Windows** (zip first to skip node_modules/bin/obj):
```powershell
# On Windows, from the Desktop:
scp -i rice-store.pem -r rice-store ubuntu@3.90.12.34:/home/ubuntu/rice-store
```
Then on the server: `cd /home/ubuntu/rice-store`

## Step 5 — Configure secrets

```bash
cp .env.example .env
nano .env
```

Fill in (see below for domain vs IP):

```
DB_PASSWORD=<strong password>
JWT_KEY=<long random 32+ char string>
EMAIL_SMTP_LOGIN=b7bc57001@smtp-brevo.com
EMAIL_SMTP_KEY=<your xsmtpsib- key>
EMAIL_FROM=yaldandanarsireddy@gmail.com
SITE_DOMAIN=ricestore.example.com
FRONTEND_GATEWAY_URL=https://api.ricestore.example.com
```

Save with Ctrl+O, Enter, Ctrl+X.

## Step 6 — Point your domain at the server (for HTTPS)

If you have a domain, add two DNS **A records** (in Route 53 or your registrar):

| Record                        | Type | Value          |
|-------------------------------|------|----------------|
| `ricestore.example.com`       | A    | `3.90.12.34`   |
| `api.ricestore.example.com`   | A    | `3.90.12.34`   |

Wait a few minutes for DNS to propagate. Caddy will then auto-fetch HTTPS certs.

## Step 7 — Launch everything

```bash
docker compose -f docker-compose.prod.yml --env-file .env up --build -d
```

First build takes 5–10 min. Watch progress:

```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f
```

## Step 8 — Authorize the server IP in Brevo

Email now sends from the **EC2 server's** IP. In Brevo → SMTP & API → SMTP →
Authorized IPs, add `3.90.12.34` (or disable the IP restriction). Otherwise
you'll get `525 Unauthorized IP address`.

## Step 9 — Open the site

- With a domain: **https://ricestore.example.com**
- Without a domain: **http://3.90.12.34** (and set FRONTEND_GATEWAY_URL to
  `http://3.90.12.34:8000`, and keep port 8000 open in the security group)

Share that URL. Done.

---

## No domain yet? (HTTP-only quick start)

If you just want to test on the raw IP without buying a domain:

1. In `.env`: `FRONTEND_GATEWAY_URL=http://3.90.12.34:8000`
2. Use the **non-Caddy** compose instead: `docker-compose.prod.yml` still works
   if you skip Caddy — but simplest is the original `docker-compose.yml` plus
   the frontend. Ask me and I'll give you an HTTP-only compose variant.
3. Open port 80 and 8000 in the security group.
4. Visit `http://3.90.12.34`.

HTTPS (Caddy + domain) is strongly recommended for anything real, since login
tokens and payment data travel over the network.

---

## Cost control on AWS

- **Stop** the instance when not demoing (EC2 → Instances → Stop). You're not
  charged for compute while stopped (only a little for storage).
- **Start** it again when needed; the public IP may change unless you attach an
  **Elastic IP** (free while attached to a running instance).
- Set a **billing alarm** (Billing → Budgets) so you're warned before charges.

## Managing the app

```bash
# view logs
docker compose -f docker-compose.prod.yml logs -f gateway

# restart after code changes
git pull
docker compose -f docker-compose.prod.yml --env-file .env up --build -d

# stop (data persists in volumes)
docker compose -f docker-compose.prod.yml down
```
