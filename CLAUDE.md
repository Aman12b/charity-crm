# CLAUDE.md – Instructions for Claude Code

You are developing the **Charity CRM App**, a membership-fee management system for a charity organization.
This is a learning/portfolio project, not a commercial product. Favor clarity and correctness over cleverness.

## Read first (in this order)

1. `docs/REQUIREMENTS.md` – functional requirements with IDs and acceptance criteria
2. `docs/NON_FUNCTIONAL_REQUIREMENTS.md` – quality, security, operations
3. `docs/ARCHITECTURE.md` – tech stack, project structure, data model, API, Docker setup
4. `docs/ROADMAP.md` – phased plan with checkboxes; this is your work queue

The requirement IDs (e.g. `FR-MEM-03`) are the source of truth. Reference them in commit messages and test names.

## How to work

- Work **phase by phase** following `docs/ROADMAP.md`. Do not start a phase before the previous one builds, runs in Docker and its tests pass.
- After finishing a task, tick its checkbox in `docs/ROADMAP.md`.
- If a requirement is unclear or contradicts another, **stop and ask** instead of guessing. Do not silently change requirements.
- Keep scope tight: no features beyond the documents (no donations, no member portal, no multi-tenancy).
- After each phase, verify with `docker compose up --build` that the whole stack starts cleanly.

## Tech stack (fixed)

- Backend: **.NET 10**, ASP.NET Core (Minimal APIs grouped per feature), EF Core + Npgsql, ASP.NET Core Identity
- Database: **PostgreSQL 17**
- PDF: **QuestPDF** (Community license) · Email: **MailKit**
- Frontend: **React + TypeScript + Vite**, React Router, TanStack Query, Mantine UI, react-hook-form + zod
- Dev mail: **Mailpit** · Reverse proxy / static hosting: **nginx**
- Tests: xUnit, FluentAssertions, Testcontainers (PostgreSQL) for integration tests; Vitest for frontend logic

## Conventions

- **Language:** code, comments, identifiers, API and commits in English. **All UI text in German.**
- C#: nullable reference types enabled, `async` all the way, file-scoped namespaces, records for DTOs.
- No business logic in endpoints. Endpoints → service in `CharityCrm.Core` → repository/DbContext.
- **Authorization is enforced server-side** via named policies (see ARCHITECTURE §6). The frontend only hides UI; it never decides access.
- Money: `decimal` mapped to `numeric(10,2)`, currency always EUR.
- Dates without time: `DateOnly`. Timestamps: `DateTimeOffset` stored in UTC.
- Every state-changing action writes an audit log entry (FR-AUD-01).
- Validation: FluentValidation on the backend, zod on the frontend; backend validation is authoritative.
- Errors returned as RFC 7807 `ProblemDetails`.
- Database changes only via EF Core migrations, never manual SQL.
- Never commit secrets. Configuration comes from environment variables (see `.env.example`).

## Commands

```bash
cp .env.example .env              # first time only, then edit secrets
docker compose up --build         # full stack
# App:     http://localhost:8080
# Mailpit: http://localhost:8025

# Backend (local)
cd backend && dotnet build && dotnet test
dotnet ef migrations add <Name> -p src/CharityCrm.Infrastructure -s src/CharityCrm.Api

# Frontend (local)
cd frontend && npm install && npm run dev && npm test && npm run lint
```

## Definition of Done (per task)

- Acceptance criteria of the referenced requirement(s) met
- Unit tests for services; integration test for each endpoint including the **403 case for unauthorized roles**
- `dotnet build` without warnings, `npm run lint` clean
- Stack starts with `docker compose up --build`
- Roadmap checkbox ticked
