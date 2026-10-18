# Roadmap

Work top to bottom. A phase is done when all its boxes are ticked, `docker compose up --build` runs cleanly and all tests pass.

## Phase 0 – Foundation
- [x] Create repository structure as in ARCHITECTURE §3
- [x] Backend solution with projects Api, Core, Infrastructure + two test projects; `TreatWarningsAsErrors`, nullable enabled
- [x] Frontend: Vite React TS, Mantine, React Router, TanStack Query, ESLint + Prettier, Vitest
- [x] EF Core DbContext with Npgsql, first (empty) migration, auto-migrate on startup (NFR-OPS-02)
- [x] Serilog JSON logging, `GET /api/health` with DB check (NFR-OPS-03/04)
- [x] OpenAPI + Scalar in Development (NFR-MAINT-04)
- [x] ProblemDetails error handling, FluentValidation pipeline
- [x] Dockerfiles (backend, frontend), nginx.conf, `docker-compose.yml`, `.env.example` (NFR-OPS-01/06)
- [x] Integration test infrastructure: WebApplicationFactory + Testcontainers PostgreSQL
- [x] Frontend layout shell: German app layout with sidebar navigation, de-AT formatting helpers

## Phase 1 – Authentication & users
- [ ] Identity with `AppUser`, roles, cookie auth, lockout, password policy (FR-AUTH-01..04, NFR-SEC-02/03)
- [ ] Persisted Data Protection keys, ForwardedHeaders, anti-forgery (NFR-SEC-02)
- [ ] Login rate limiting (NFR-SEC-07)
- [ ] Seed initial admin from env (FR-AUTH-07)
- [ ] Change password + forced password change filter (FR-AUTH-05/06)
- [ ] Authorization policies AdminOnly / MemberRead / MemberWrite / Finance (ARCHITECTURE §6)
- [ ] Audit log entity + `IAuditLogger`; log logins (FR-AUD-01)
- [ ] User management endpoints incl. last-admin protection and session invalidation (FR-USR-01..06)
- [ ] Frontend: login page, AuthProvider, route guards, change-password page, user management pages
- [ ] Integration tests: auth flows, lockout, 401/403 matrix for user endpoints

## Phase 2 – Members
- [ ] Member entity, member number sequence, indexes, migration
- [ ] Create with duplicate check, edit with optimistic concurrency, de-register, reactivate (FR-MEM-01, 04..06)
- [ ] List with search, filter, pagination; details (FR-MEM-02/03)
- [ ] CSV export (FR-MEM-07)
- [ ] Audit entries for all member changes
- [ ] Frontend: member list, create/edit form (zod), detail page, de-register dialog
- [ ] Tests: validation rules, duplicate warning, Cashier read-only (403 on write), Secretary access

## Phase 3 – Settings, payments & invoices
- [ ] OrganizationSettings entity + seed defaults, settings endpoints and page (FR-FEE-01/02)
- [ ] Payment entity, period calculation, overlap check (FR-PAY-01)
- [ ] InvoiceNumberSequence with row lock, gap-free numbering (FR-INV-01, NFR-REL-03)
- [ ] Invoice entity with snapshots; QuestPDF document; file storage (FR-INV-01)
- [ ] Record payment as single transaction (NFR-REL-01)
- [ ] Payment list with filters and sum (FR-PAY-02)
- [ ] Invoice list, detail, PDF download (FR-INV-02/03)
- [ ] Cancel invoice incl. "STORNIERT" PDF (FR-INV-04)
- [ ] Frontend: settings page, record-payment form (member search, period picker, prefilled amount), payment list, invoice list + detail
- [ ] Tests: concurrent invoice numbering (parallel requests → unique, gap-free), overlap rules, cancellation frees period, Secretary gets 403

## Phase 4 – Email delivery
- [ ] SMTP options, MailKit `IEmailSender`, Mailpit in compose (NFR-OPS-05)
- [ ] Outbox entity, enqueue in payment transaction (FR-MAIL-01)
- [ ] `EmailOutboxWorker` with retries/backoff and status updates (FR-MAIL-02, NFR-REL-02)
- [ ] German HTML + text template (FR-MAIL-04)
- [ ] Resend endpoint + dialog (FR-MAIL-03)
- [ ] Email status shown in invoice list/detail
- [ ] Tests: worker success/failure/retry with fake sender; manual check in Mailpit UI

## Phase 5 – Reports & audit UI
- [ ] Dashboard per role (FR-REP-01)
- [ ] Open contributions report (FR-REP-02)
- [ ] Payment summary per month + CSV (FR-REP-03)
- [ ] Audit log page with filters (FR-AUD-02)
- [ ] Tests for report calculations (monthly vs yearly payers, cancelled payments excluded)

## Phase 6 – Hardening
- [ ] Security headers verified, CSP works with built app (NFR-SEC-06)
- [ ] Accessibility pass: labels, keyboard navigation, contrast (NFR-UX-05)
- [ ] Performance check with seeded 5,000 members / 100,000 payments (NFR-PERF-01..03); optional dev seed command
- [ ] `scripts/backup.sh` / `restore.sh` for DB + invoice volume (NFR-REL-04)
- [ ] README: setup, roles, screenshots, how to switch to a real SMTP provider
