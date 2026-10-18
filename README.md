# Charity CRM App

Membership-fee management for a charity organization. Staff manage member records, record monthly or yearly
membership contributions, and the system generates PDF invoices and emails them to members — all within a
role-based access system.

> Learning / portfolio project. Not intended for commercial use.

## Documentation

| Document | Description |
|---|---|
| [Requirements](docs/REQUIREMENTS.md) | Functional requirements, roles, permission matrix, acceptance criteria |
| [Non-Functional Requirements](docs/NON_FUNCTIONAL_REQUIREMENTS.md) | Security, performance, reliability, operations |
| [Architecture](docs/ARCHITECTURE.md) | Tech stack, containers, data model, API, flows, Docker setup |
| [Roadmap](docs/ROADMAP.md) | Phased development plan with task checklists |
| [CLAUDE.md](CLAUDE.md) | Working instructions for Claude Code |

## Quick start

```bash
cp .env.example .env     # set POSTGRES_PASSWORD and SEED_ADMIN_PASSWORD
docker compose up --build
```

| Service | URL |
|---|---|
| Web app | http://localhost:8080 |
| Mailpit (captured emails) | http://localhost:8025 |

Log in with `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` from `.env`. You will be asked to change the password on first login.

## Tech stack

.NET 10 · ASP.NET Core · EF Core · PostgreSQL 17 · React + TypeScript (Vite) · Mantine · QuestPDF · MailKit · nginx · Mailpit · Docker Compose
