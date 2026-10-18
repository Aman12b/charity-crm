# Deployment – Proxmox home server

```
User ──https──> Cloudflare (DNS, proxied, TLS) ──> Nginx Proxy Manager ──http──> LXC "charity-crm" :8080 (web → api → db)
GitHub push to master ──> Actions: tests → build images → GHCR ──> self-hosted runner in the LXC: pull + up -d
```

The home server needs **no inbound port for the pipeline**: the self-hosted runner connects outward to GitHub.

## 1. LXC on Proxmox (once)

- Debian 12/13 LXC, unprivileged, **Nesting** enabled (needed for Docker), 2 vCPU, 2–4 GB RAM, 20 GB disk.
- Static IP (e.g. `192.168.1.50`) via DHCP reservation or in the LXC network settings.
- Install Docker Engine + Compose plugin (official Docker apt repo).

```bash
adduser --disabled-password deploy && usermod -aG docker deploy
mkdir -p /opt/charity-crm && chown deploy:deploy /opt/charity-crm
```

## 2. `.env` on the server (once, never in Git)

`/opt/charity-crm/.env`, owner `deploy`, `chmod 600`:

```dotenv
POSTGRES_PASSWORD=<long random>          # openssl rand -base64 32
SEED_ADMIN_EMAIL=you@yourdomain.at
SEED_ADMIN_PASSWORD=<strong, changed on first login>
WEB_PORT=8080
SMTP_HOST=smtp-relay.brevo.com           # or another provider
SMTP_PORT=587
SMTP_USERNAME=...
SMTP_PASSWORD=...
SMTP_USE_STARTTLS=true
SMTP_FROM_ADDRESS=verified-sender@yourdomain.at
SMTP_FROM_NAME=My Charity
AUTH_COOKIE_SECURE=true
```

## 3. Self-hosted GitHub runner in the LXC (once)

GitHub → repo → Settings → Actions → Runners → *New self-hosted runner* (Linux x64). As user `deploy`:

```bash
mkdir ~/actions-runner && cd ~/actions-runner
# download + extract as shown on the GitHub page, then:
./config.sh --url https://github.com/Aman12b/charity-crm --token <TOKEN> --labels charity-crm --unattended
sudo ./svc.sh install deploy && sudo ./svc.sh start
```

**Public repo – important:** Settings → Actions → General → *Fork pull request workflows from outside
collaborators* → **Require approval for all outside collaborators**. Otherwise a fork PR could change the workflow
and run code on your home server. (Alternative: make the repo private.)

Optional: Settings → Environments → `production` → *Required reviewers* to approve each deployment manually.

## 4. Cloudflare + Nginx Proxy Manager

1. Cloudflare DNS: `A crm → <your public IP>` (or CNAME to your DynDNS name), **Proxied** (orange cloud).
2. Cloudflare SSL/TLS mode: **Full (strict)**.
3. NPM → Proxy Host: domain `crm.yourdomain.at`, forward `http://192.168.1.50:8080`, *Block common exploits* on,
   SSL: Let's Encrypt via DNS challenge (Cloudflare API token) or a Cloudflare Origin Certificate, *Force SSL* on.
4. Restrict port 8080 on the LXC to the NPM host (Proxmox firewall or `iptables`), so nobody bypasses the proxy
   and spoofs `CF-Connecting-IP`. Ideally also only allow Cloudflare IP ranges to reach NPM on 443.

## 5. Day-to-day

- Deploy: push/merge to `master`. A backup is taken before every deployment (`/opt/charity-crm/backups`, last 14 kept).
- Rollback: Actions → CI/CD → *Run workflow* → `tag` = previous commit SHA.
- Manual restart with the deployed version: `cd /opt/charity-crm && IMAGE_TAG=$(cat .image-tag) docker compose up -d`
- Logs: `docker compose logs -f api`
- Offsite backup: copy `/opt/charity-crm/backups` elsewhere (Proxmox Backup Server snapshot of the LXC, rclone, …).
