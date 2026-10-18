# Architecture

## 1. Overview

```
                    ┌──────────────────────────────────────────────────────┐
 Browser ──HTTP──▶  │ web (nginx)                                          │
  :8080             │  • serves React SPA (static build)                   │
                    │  • proxies /api/* ──▶ api:8080                       │
                    └───────────────┬──────────────────────────────────────┘
                                    │
                    ┌───────────────▼──────────────────────────────────────┐
                    │ api (ASP.NET Core, .NET 10)                          │
                    │  • REST API, Identity cookie auth, policies          │
                    │  • EmailOutboxWorker (BackgroundService)             │
                    │  • QuestPDF invoice rendering                        │
                    └──────┬──────────────────────┬───────────────┬────────┘
                           │ EF Core              │ SMTP          │ files
                    ┌──────▼──────┐        ┌──────▼──────┐  ┌─────▼──────────┐
                    │ db          │        │ mailpit     │  │ volume:        │
                    │ PostgreSQL  │        │ (dev SMTP + │  │ /data/invoices │
                    │ 17          │        │  web UI)    │  │ /data/keys     │
                    └─────────────┘        └─────────────┘  └────────────────┘
                                             UI :8025
```

Frontend and API are served from the **same origin** (via nginx), so cookie auth works without CORS.

## 2. Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Minimal APIs, ASP.NET Core Identity (cookies) |
| Data access | EF Core 10 + Npgsql, migrations |
| Validation | FluentValidation |
| PDF | QuestPDF (Community license) |
| Email | MailKit |
| Logging | Serilog (JSON to stdout) |
| API docs | Built-in OpenAPI + Scalar (Development only) |
| Database | PostgreSQL 17 |
| Frontend | React, TypeScript, Vite, React Router, TanStack Query, Mantine, react-hook-form + zod, dayjs |
| Tests | xUnit, FluentAssertions, Testcontainers, WebApplicationFactory; Vitest + Testing Library |
| Runtime | Docker Compose, nginx, Mailpit |

## 3. Repository structure

```
charity-crm/
├── CLAUDE.md
├── README.md
├── docker-compose.yml
├── .env.example
├── docs/
├── scripts/
│   ├── backup.sh
│   └── restore.sh
├── backend/
│   ├── Dockerfile
│   ├── CharityCrm.sln
│   ├── src/
│   │   ├── CharityCrm.Api/              # endpoints, auth setup, DI, Program.cs
│   │   │   └── Features/{Auth,Users,Members,Payments,Invoices,Settings,Reports,Audit}/
│   │   ├── CharityCrm.Core/             # entities, enums, services, interfaces, validators
│   │   └── CharityCrm.Infrastructure/   # DbContext, migrations, PDF, email, file storage, worker
│   └── tests/
│       ├── CharityCrm.Core.Tests/
│       └── CharityCrm.Api.IntegrationTests/
└── frontend/
    ├── Dockerfile
    ├── nginx.conf
    └── src/
        ├── api/            # typed API client, query hooks
        ├── auth/           # session context, route guards
        ├── features/       # members, payments, invoices, users, settings, reports, audit
        ├── components/     # shared UI
        └── lib/            # formatting (de-AT), zod schemas
```

Dependency rule: `Api → Core ← Infrastructure` (Infrastructure implements Core interfaces; Api wires both).

## 4. Data model

```
AppUser (Identity) 1───* AuditLog
Member 1───* Payment 1───1 Invoice 1───* EmailOutboxMessage
OrganizationSettings (single row)
InvoiceNumberSequence (one row per year)
```

### AppUser (extends IdentityUser<Guid>)
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| Email / NormalizedEmail | text | unique |
| FirstName, LastName | varchar(100) | |
| Role | varchar(20) | `Admin` / `Cashier` / `Secretary` (also stored as Identity role claim) |
| IsActive | bool | |
| MustChangePassword | bool | |
| LastLoginAt | timestamptz? | |
| CreatedAt | timestamptz | |

### Member
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| MemberNumber | varchar(10) | unique, `M-000001`, from DB sequence `member_number_seq` |
| FirstName, LastName | varchar(100) | indexed (lower) |
| DateOfBirth | date | |
| Street | varchar(200) | |
| PostalCode | varchar(10) | |
| City | varchar(100) | |
| Country | varchar(100) | default `Österreich` |
| Email | varchar(254)? | indexed (lower) |
| Phone | varchar(30)? | |
| BillingInterval | varchar(10) | `Monthly` / `Yearly` |
| Status | varchar(10) | `Active` / `Inactive` |
| JoinDate | date | |
| LeaveDate | date? | |
| LeaveReason | varchar(500)? | |
| Notes | varchar(2000)? | |
| CreatedAt, UpdatedAt | timestamptz | |
| RowVersion | xmin | optimistic concurrency |

### Payment
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| MemberId | uuid | FK → Member |
| Interval | varchar(10) | interval at time of payment |
| PeriodStart, PeriodEnd | date | month: 1st–last day · year: 01.01.–31.12. |
| Amount | numeric(10,2) | > 0 |
| PaymentDate | date | |
| Method | varchar(20) | `BankTransfer` / `Cash` / `Other` |
| Reference | varchar(200)? | |
| Status | varchar(10) | `Recorded` / `Cancelled` |
| RecordedByUserId | uuid | FK → AppUser |
| CreatedAt | timestamptz | |

Overlap rule (FR-PAY-01) is checked in `PaymentService` inside the transaction: no other `Recorded` payment of the same member with an overlapping `[PeriodStart, PeriodEnd]`. Optionally enforce with a PostgreSQL exclusion constraint (`btree_gist`, `daterange`, `WHERE status = 'Recorded'`).

### Invoice
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| InvoiceNumber | varchar(12) | unique, `YYYY-00001` |
| PaymentId | uuid | FK → Payment, unique |
| MemberId | uuid | FK → Member |
| IssueDate | date | |
| Amount | numeric(10,2) | copied from payment |
| PeriodStart, PeriodEnd | date | copied |
| MemberSnapshot | jsonb | name, member number, address, email at issue time |
| OrganizationSnapshot | jsonb | org data + footer at issue time |
| Status | varchar(10) | `Issued` / `Cancelled` |
| CancelledAt | timestamptz? | |
| CancellationReason | varchar(500)? | |
| PdfPath | varchar(300) | relative path in `/data/invoices/{year}/{number}.pdf` |
| EmailStatus | varchar(15) | `NotApplicable` / `Pending` / `Sent` / `Failed` |
| EmailSentAt | timestamptz? | |
| EmailLastError | varchar(1000)? | |
| CreatedAt | timestamptz | |

### InvoiceNumberSequence
| Column | Type |
|---|---|
| Year | int (PK) |
| LastNumber | int |

Allocation: `SELECT ... FOR UPDATE` on the year's row (insert if missing), increment, use – all inside the payment transaction. This guarantees gap-free numbers because a rollback also rolls back the increment.

### EmailOutboxMessage
| Column | Type | Notes |
|---|---|---|
| Id | uuid | PK |
| InvoiceId | uuid | FK |
| ToAddress | varchar(254) | |
| Attempts | int | |
| NextAttemptAt | timestamptz | |
| Status | varchar(10) | `Pending` / `Sent` / `Failed` |
| LastError | varchar(1000)? | |
| CreatedAt, ProcessedAt | timestamptz | |

### OrganizationSettings (single row, Id = 1)
Name, Street, PostalCode, City, Country, Email, Phone, Iban, Bic, ZvrNumber, InvoiceFooterText, MonthlyFee numeric(10,2), YearlyFee numeric(10,2), UpdatedAt.

### AuditLog
| Column | Type |
|---|---|
| Id | bigint identity |
| Timestamp | timestamptz |
| UserId | uuid? (null for failed login of unknown email) |
| Action | varchar(50) (e.g. `Member.Created`, `Invoice.Cancelled`, `Auth.LoginFailed`) |
| EntityType | varchar(50)? |
| EntityId | varchar(50)? |
| Changes | jsonb? (`{ "field": { "old": ..., "new": ... } }`) |

Written by an `IAuditLogger` service called from Core services (explicit, not via EF interceptor, to keep it readable).

## 5. Key flows

### 5.1 Record payment → invoice → email

```
Cashier ─POST /api/payments─▶ PaymentService.RecordAsync
  BEGIN TRANSACTION
    1. load member, ensure Active
    2. compute period from interval + selection, check overlap
    3. insert Payment
    4. allocate invoice number (row lock on InvoiceNumberSequence)
    5. build snapshots, insert Invoice (EmailStatus = Pending | NotApplicable)
    6. render PDF (QuestPDF) and write to /data/invoices/... *
    7. if member email: insert EmailOutboxMessage
    8. write AuditLog
  COMMIT
  ◀─ 201 { paymentId, invoiceId, invoiceNumber }

EmailOutboxWorker (every 10 s):
  pick Pending messages with NextAttemptAt <= now (FOR UPDATE SKIP LOCKED, batch 10)
  send via MailKit with PDF attachment
  success → message Sent, invoice EmailStatus Sent + EmailSentAt
  failure → Attempts++, NextAttemptAt = now + 2^Attempts minutes; after 5 → Failed, invoice EmailStatus Failed
```
\* If the PDF write fails, the transaction rolls back. If the commit fails after the file was written, an orphaned file remains – acceptable; the file is overwritten on retry because the path is derived from the (re-allocated) number. Alternatively, PDFs can be regenerated on demand from the snapshots; the stored file is a cache.

### 5.2 Cancel invoice
`POST /api/invoices/{id}/cancel { reason }` → invoice `Cancelled`, payment `Cancelled`, PDF re-rendered with "STORNIERT" watermark, audit entry. No email is sent automatically.

### 5.3 Resend invoice
`POST /api/invoices/{id}/resend { email? }` → new outbox message, invoice EmailStatus `Pending`.

### 5.4 Login
`POST /api/auth/login` → Identity `PasswordSignInAsync` (lockout enabled) → checks `IsActive` → issues cookie → updates `LastLoginAt` → audit. Response includes `mustChangePassword`; while true, a middleware/filter blocks every endpoint except `/api/auth/me`, `/api/auth/change-password`, `/api/auth/logout` with `403 { code: "PASSWORD_CHANGE_REQUIRED" }`.

Deactivation invalidates sessions by updating the user's security stamp; `SecurityStampValidator` interval set to 1 minute.

## 6. Authorization

Policies (role claim based):

| Policy | Roles |
|---|---|
| `AdminOnly` | Admin |
| `MemberRead` | Admin, Cashier, Secretary |
| `MemberWrite` | Admin, Secretary |
| `Finance` | Admin, Cashier |

Every endpoint group calls `.RequireAuthorization(policy)`. Integration tests assert the full matrix.

## 7. API (all under `/api`, JSON, ProblemDetails on errors)

| Method | Route | Policy | Req |
|---|---|---|---|
| POST | /auth/login | anonymous | AUTH-01 |
| POST | /auth/logout | authenticated | AUTH-02 |
| GET | /auth/me | authenticated | AUTH-03 |
| POST | /auth/change-password | authenticated | AUTH-05/06 |
| GET | /auth/antiforgery | anonymous | SEC-02 |
| GET | /users | AdminOnly | USR-02 |
| POST | /users | AdminOnly | USR-01 |
| GET | /users/{id} | AdminOnly | USR-02 |
| PUT | /users/{id} | AdminOnly | USR-03 |
| POST | /users/{id}/deactivate · /reactivate | AdminOnly | USR-04 |
| POST | /users/{id}/reset-password | AdminOnly | USR-05 |
| GET | /members?search=&status=&interval=&page= | MemberRead | MEM-02 |
| GET | /members/export.csv | MemberWrite | MEM-07 |
| GET | /members/{id} | MemberRead | MEM-03 |
| POST | /members/duplicate-check | MemberWrite | MEM-01 |
| POST | /members | MemberWrite | MEM-01 |
| PUT | /members/{id} | MemberWrite | MEM-04 |
| POST | /members/{id}/deregister · /reactivate | MemberWrite | MEM-05/06 |
| GET | /members/{id}/payments · /invoices | Finance | MEM-03 |
| GET | /payments?from=&to=&memberId=&method=&page= | Finance | PAY-02 |
| POST | /payments | Finance | PAY-01 |
| GET | /invoices?... | Finance | INV-02 |
| GET | /invoices/{id} | Finance | INV-02 |
| GET | /invoices/{id}/pdf | Finance | INV-02 |
| POST | /invoices/{id}/cancel | Finance | INV-04 |
| POST | /invoices/{id}/resend | Finance | MAIL-03 |
| GET | /settings | Finance (read) | FEE-01 |
| PUT | /settings | AdminOnly | FEE-01/02 |
| GET | /reports/dashboard | authenticated (role-filtered) | REP-01 |
| GET | /reports/open-contributions?year=&month= | Finance | REP-02 |
| GET | /reports/payment-summary?year= (+ .csv) | Finance | REP-03 |
| GET | /audit?userId=&entityType=&entityId=&from=&to=&page= | AdminOnly | AUD-02 |
| GET | /health | anonymous | OPS-03 |

Pagination response: `{ items: [...], page, pageSize, totalCount }`.

## 8. Frontend

Routes (German labels):

| Route | Page | Visible for |
|---|---|---|
| /login | Anmeldung | – |
| /passwort-aendern | Passwort ändern | all |
| / | Dashboard | all |
| /mitglieder, /mitglieder/neu, /mitglieder/:id | Mitglieder | all (edit: Admin, Secretary) |
| /zahlungen, /zahlungen/neu | Zahlungen | Admin, Cashier |
| /rechnungen, /rechnungen/:id | Rechnungen | Admin, Cashier |
| /berichte | Offene Beiträge, Zahlungsübersicht | Admin, Cashier |
| /benutzer | Benutzerverwaltung | Admin |
| /einstellungen | Organisation & Beiträge | Admin |
| /protokoll | Audit-Log | Admin |

- `AuthProvider` loads `/api/auth/me` on start; `RequireRole` guard for routes; 401 from any request → redirect to login.
- API client sends `credentials: 'include'` and the `X-XSRF-TOKEN` header.
- Formatting helpers with `Intl` for `de-AT` (dates, EUR).

## 9. Docker setup

### docker-compose.yml
See `docker-compose.yml` in the repository root (services: `db`, `api`, `web`, `mailpit`; volumes: `pgdata`, `appdata`).

### backend/Dockerfile (multi-stage)
- Build stage: `mcr.microsoft.com/dotnet/sdk:10.0`, restore (copy `.csproj` files first for layer caching), `dotnet publish -c Release -o /app`.
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0`, `USER app` (non-root), `EXPOSE 8080`, create `/data/invoices` and `/data/keys` owned by `app`.
- QuestPDF needs no native dependencies beyond the base image; if fonts are missing, install `fontconfig` + a font package (e.g. `fonts-dejavu-core`) in the runtime stage.

### frontend/Dockerfile (multi-stage)
- Build stage: `node:22-alpine`, `npm ci`, `npm run build`.
- Runtime stage: `nginx:alpine`, copy `dist/` to `/usr/share/nginx/html`, copy `nginx.conf`.

### frontend/nginx.conf (essentials)
```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  client_max_body_size 2m;

  location /api/ {
    proxy_pass http://api:8080;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }

  location / {
    try_files $uri /index.html;
  }

  add_header X-Content-Type-Options nosniff always;
  add_header X-Frame-Options DENY always;
  add_header Referrer-Policy strict-origin-when-cross-origin always;
  add_header Content-Security-Policy "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'" always;
}
```
The API must use `ForwardedHeaders` middleware so `Secure` cookies and scheme detection work behind the proxy.

### Configuration (environment variables → ASP.NET config keys)
| Env var | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection |
| `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__UseStartTls` | Mail transport |
| `Smtp__FromAddress`, `Smtp__FromName` | Sender |
| `Storage__InvoicePath` | PDF storage (default `/data/invoices`) |
| `DataProtection__KeyPath` | Persisted cookie keys (default `/data/keys`) |
| `Seed__AdminEmail`, `Seed__AdminPassword` | Initial admin (FR-AUTH-07) |
| `Auth__CookieSecure` | `false` for local HTTP, `true` behind HTTPS |
| `Serilog__MinimumLevel__Default` | Log level |

### Switching to a real mail provider
Set `SMTP_HOST`, `SMTP_PORT=587`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_USE_STARTTLS=true` and a verified sender address in `.env`. No code changes.
