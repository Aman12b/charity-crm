# Functional Requirements

## 1. Scope

The Charity CRM App manages **membership contributions** of a charity organization.

**In scope:** staff user management with roles, member records, monthly/yearly membership fees, payment recording,
PDF invoice generation, email delivery of invoices, basic reports, audit log.

**Out of scope (v1):** donations, member self-service portal / member logins, online payment (card, PayPal),
SEPA direct debit, dunning letters, accounting system integration, multi-organization support, VAT handling.

## 2. Glossary

| Term | Meaning |
|---|---|
| **User** | A staff member who logs into the system. Has exactly one role. |
| **Member** | A person whose record is maintained by the organization. Members never log in. |
| **Billing interval** | How a member pays: `Monthly` or `Yearly`. |
| **Contribution period** | The time span a payment covers: one calendar month (monthly) or one calendar year (yearly). |
| **Payment** | A recorded receipt of money from a member for one contribution period. |
| **Invoice** | A document generated automatically for every payment (functions as a payment confirmation). |
| **Fee settings** | The organization-wide default amounts for monthly and yearly contributions. |

## 3. Roles & permission matrix

Each user has **exactly one** role.

| Capability | Admin | Cashier | Secretary |
|---|:-:|:-:|:-:|
| Manage users (create, edit, deactivate, reset password, assign role) | ✅ | ❌ | ❌ |
| Edit organization & fee settings | ✅ | ❌ | ❌ |
| View audit log | ✅ | ❌ | ❌ |
| View members (list, search, details) | ✅ | ✅ (read-only) | ✅ |
| Create / edit / de-register / reactivate members | ✅ | ❌ | ✅ |
| Record payments | ✅ | ✅ | ❌ |
| View / download / resend / cancel invoices | ✅ | ✅ | ❌ |
| View open contributions & payment reports | ✅ | ✅ | ❌ |
| Export member list (CSV) | ✅ | ❌ | ✅ |
| Change own password, log in/out | ✅ | ✅ | ✅ |

> Cashiers need read access to members to assign payments. Secretaries do not see any financial data.

## 4. Requirements

Format: **ID – title**, description, acceptance criteria (AC).

### 4.1 Authentication (AUTH)

**FR-AUTH-01 – Login**
Users log in with email and password.
- AC: Valid credentials of an active user create a session and redirect to the dashboard.
- AC: Invalid credentials show a generic error ("E-Mail oder Passwort falsch") without revealing which field was wrong.
- AC: Deactivated users cannot log in.

**FR-AUTH-02 – Logout**
- AC: Logout ends the session server-side; protected pages are no longer accessible.

**FR-AUTH-03 – Session management**
- AC: Session expires after 30 minutes of inactivity (sliding); user is redirected to login.
- AC: Unauthenticated requests to any API endpoint except login return `401`.

**FR-AUTH-04 – Account lockout**
- AC: After 5 failed login attempts the account is locked for 15 minutes.

**FR-AUTH-05 – Change own password**
- AC: Requires the current password; new password must meet the password policy (NFR-SEC-03).

**FR-AUTH-06 – Forced password change**
- AC: Users created by an admin or whose password was reset by an admin must change their password on next login before accessing anything else.

**FR-AUTH-07 – Initial admin**
- AC: On first start with an empty database, an admin account is created from environment variables `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD`, flagged for forced password change.

### 4.2 User management (USR) – Admin only

**FR-USR-01 – Create user**
Fields: first name, last name, email (unique), role, temporary password.
- AC: Email must be unique (case-insensitive).
- AC: New user is active and flagged for forced password change.

**FR-USR-02 – List & view users**
- AC: List shows name, email, role, status (active/inactive), last login; filterable by role and status.

**FR-USR-03 – Edit user**
- AC: Admin can change name, email and role.

**FR-USR-04 – Deactivate / reactivate user**
Users are never hard-deleted (audit log references them).
- AC: A deactivated user's active sessions are invalidated immediately.
- AC: Deactivated users can be reactivated.

**FR-USR-05 – Reset password**
- AC: Admin sets a temporary password; user is flagged for forced password change.

**FR-USR-06 – Last-admin protection**
- AC: The system rejects any action (deactivate, role change) that would leave zero active admins.
- AC: An admin cannot deactivate themselves.

### 4.3 Member management (MEM)

**FR-MEM-01 – Register member** (Secretary, Admin)
| Field | Rules |
|---|---|
| Member number | Auto-generated, unique, sequential, format `M-000001`, immutable |
| First name, last name | Required, max 100 chars each |
| Date of birth | Required, in the past, age ≤ 120 years |
| Street + house number, postal code, city, country | Required; country defaults to "Österreich" |
| Email | Optional, valid format |
| Phone | Optional, max 30 chars |
| Billing interval | Required: `Monthly` or `Yearly` |
| Join date | Required, defaults to today |
| Notes | Optional, max 2000 chars |
- AC: Member is created with status `Active`.
- AC: If a member with the same first name, last name and date of birth already exists, a warning is shown; the user may confirm and proceed.

**FR-MEM-02 – List & search members** (all roles)
- AC: Paginated list (25 per page) with member number, name, city, billing interval, status.
- AC: Search by name, member number or email (case-insensitive, partial match).
- AC: Filter by status and billing interval; default filter: `Active`.

**FR-MEM-03 – Member details** (all roles)
- AC: Shows all member fields.
- AC: For Admin and Cashier, additionally shows the member's payments and invoices.

**FR-MEM-04 – Edit member** (Secretary, Admin)
- AC: All fields except member number are editable.
- AC: Changing the billing interval affects only future payments.

**FR-MEM-05 – De-register member** (Secretary, Admin)
Members with payment history cannot be physically deleted, so de-registration sets status to `Inactive`.
- AC: Requires a leave date (default today) and an optional reason.
- AC: Inactive members cannot receive new payments.
- AC: Existing payments and invoices remain unchanged and accessible.

**FR-MEM-06 – Reactivate member** (Secretary, Admin)
- AC: Sets status back to `Active` and clears the leave date.

**FR-MEM-07 – Export members** (Secretary, Admin)
- AC: CSV export (UTF-8 with BOM, `;` separator for Excel compatibility) of the current filtered list.

### 4.4 Fee settings (FEE) – Admin only

**FR-FEE-01 – Default contribution amounts**
- AC: Admin can set the default monthly and yearly amounts (EUR, > 0).
- AC: Changes apply only to payments recorded after the change; existing payments/invoices are unchanged.

**FR-FEE-02 – Organization data**
- AC: Admin can edit organization name, address, email, phone, bank details (IBAN/BIC), ZVR number and an invoice footer text. These appear on invoices.

### 4.5 Payments (PAY) – Cashier, Admin

**FR-PAY-01 – Record payment**
Flow: select member → contribution period is chosen based on the member's billing interval → amount is prefilled from fee settings → choose payment method → save.
| Field | Rules |
|---|---|
| Member | Required, must be `Active` |
| Period | Monthly: month + year · Yearly: year |
| Amount | Required, > 0, prefilled, editable |
| Payment date | Required, not in the future, defaults to today |
| Payment method | `BankTransfer`, `Cash`, `Other` |
| Reference / note | Optional, max 200 chars |
- AC: Saving creates the payment **and** its invoice in a single transaction (FR-INV-01).
- AC: A member cannot have two non-cancelled payments for the same period; the system rejects it with a clear message.
- AC: If the member's billing interval is yearly, the period cannot overlap months already paid individually (and vice versa).

**FR-PAY-02 – List payments**
- AC: Paginated list filterable by date range, member, period, payment method; shows sum of listed amounts.

### 4.6 Invoices (INV) – Cashier, Admin

**FR-INV-01 – Automatic invoice generation**
Every recorded payment generates exactly one invoice.
- AC: Invoice number is sequential per year without gaps, format `YYYY-00001` (e.g. `2026-00042`), unique even under concurrent requests.
- AC: The invoice stores a **snapshot** of member name, member number and address and of organization data at issue time. Later edits do not change existing invoices.
- AC: A PDF is generated (layout: organization header, member address, invoice number, issue date, period, amount, payment method, payment date, "Betrag dankend erhalten", footer text).

**FR-INV-02 – View & download invoice**
- AC: Invoice list (filterable by number, member, date range, status, email status) and detail view.
- AC: PDF can be downloaded.

**FR-INV-03 – Invoices are immutable**
- AC: No field of an issued invoice can be edited.

**FR-INV-04 – Cancel invoice (Storno)**
- AC: Requires a reason. Sets invoice status to `Cancelled` and the linked payment to `Cancelled`.
- AC: Cancelled invoices remain visible and downloadable; the PDF is marked "STORNIERT".
- AC: The invoice number is not reused. The period becomes payable again.

### 4.7 Email delivery (MAIL)

**FR-MAIL-01 – Send invoice by email**
- AC: After an invoice is created and the member has an email address, the invoice PDF is emailed asynchronously (subject: "Ihre Rechnung {Nummer} – {Organisation}").
- AC: Email failures never roll back the payment or invoice.

**FR-MAIL-02 – Email status tracking**
- AC: Each invoice has an email status: `NotApplicable` (no email), `Pending`, `Sent`, `Failed`, with timestamp and last error message.
- AC: Failed sends are retried automatically up to 5 times with increasing delay, then marked `Failed`.

**FR-MAIL-03 – Resend invoice**
- AC: Cashier/Admin can resend an invoice manually, optionally to a different email address entered in the dialog.

**FR-MAIL-04 – Email template**
- AC: German HTML email with plain-text alternative, containing greeting with member name, invoice number, period, amount and organization contact data.

### 4.8 Reports & dashboard (REP)

**FR-REP-01 – Dashboard** (all roles, content by role)
- Secretary: number of active members, new members this month.
- Cashier/Admin: additionally payments this month (count + sum), number of failed emails, number of open contributions for the current period.

**FR-REP-02 – Open contributions** (Cashier, Admin)
- AC: Lists active members without a non-cancelled payment for the current period (current month for monthly payers, current year for yearly payers). Selectable reference month/year.

**FR-REP-03 – Payment summary** (Cashier, Admin)
- AC: Sum of payments per month for a selected year, plus yearly total; CSV export.

### 4.9 Audit log (AUD)

**FR-AUD-01 – Record changes**
- AC: Every create/update/deactivate/cancel action on users, members, payments, invoices and settings, plus logins (success and failure), is logged with timestamp, user, action, entity type, entity id and changed fields (old → new).
- AC: Passwords and password hashes are never logged.

**FR-AUD-02 – View audit log** (Admin)
- AC: Paginated, filterable by user, entity type, entity id and date range. Read-only.
