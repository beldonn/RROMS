# RROMS - Web-Based Restaurant Reservation and Order Management System

A full-stack coursework implementation of the RROMS brief: ASP.NET Core 10 Razor Pages,
Entity Framework Core, SQL Server, Bootstrap 5, and vanilla JavaScript.

Restaurant used for seed data / branding: **"Ember & Thyme"**.

---

## 1. Tech stack (matches the brief)

| Layer | Technology |
|---|---|
| Frontend | HTML, CSS, Bootstrap 5, JavaScript (+ Chart.js for analytics) |
| Backend | ASP.NET Core Web App - Razor Pages, C# |
| ORM / Data access | Entity Framework Core (Code First) |
| Database | SQL Server (LocalDB by default) |
| Auth | ASP.NET Core Identity (cookie auth, role-based) |
| IDE | Visual Studio 2026 |
| Reports export | QuestPDF (PDF), ClosedXML (Excel), built-in CSV writer |

## 2. Getting started in Visual Studio 2026

1. Open `RROMS.sln`.
2. Restore NuGet packages (Visual Studio does this automatically on open; if not,
   right-click the solution → **Restore NuGet Packages**).
3. Check the connection string in `RROMS/appsettings.json`. The default targets
   **LocalDB** (`(localdb)\mssqllocaldb`), which ships with Visual Studio, so most
   people can skip this step. To use full SQL Server / SSMS instead, change
   `ConnectionStrings:DefaultConnection` to your instance, e.g.:
   ```
   Server=localhost\SQLEXPRESS;Database=RROMS;Trusted_Connection=True;TrustServerCertificate=True
   ```
4. **Database migrations.** The project ships without a `Migrations` folder (hand-writing
   generated migration snapshots isn't reliable outside Visual Studio's tooling). Create
   the initial migration once, from the Package Manager Console with `RROMS` as the
   default project:
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```
   Alternatively, from a terminal in the `RROMS/RROMS` folder:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   If you don't have the EF Core CLI tool installed: `dotnet tool install --global dotnet-ef`.
5. Press **F5** / **Start**. On first run the app automatically applies the migration and
   seeds demo data (roles, accounts, dining areas, tables, menu, operating hours, FAQs) -
   see `Data/SeedData.cs`.
6. Log in with one of the seeded accounts shown on the Login page:

   | Role | Email | Password |
   |---|---|---|
   | Administrator | admin@rroms.local | Admin@12345 |
   | Staff | staff@rroms.local | Staff@12345 |
   | Customer | customer@rroms.local | Customer@12345 |

   Or register a new customer account from **Register**.

If your installed SDK isn't .NET 10, open `RROMS/RROMS.csproj` and change
`<TargetFramework>net10.0</TargetFramework>` to `net8.0` or `net9.0` - nothing else in the
code needs to change.

## 3. Project structure

```
RROMS.sln
RROMS/
  Program.cs                 Composition root: DI, Identity, auth policies, middleware, chatbot endpoint
  appsettings.json           Connection string, seed credentials, SMTP settings
  Data/
    ApplicationDbContext.cs  EF Core DbContext (Identity + domain entities, Fluent API)
    SeedData.cs              Migrations + roles/demo accounts/reference data seeding
  Models/                    15 entities + enums (Reservation, Order, Payment, MenuItem, ...)
  ViewModels/                Form/report/dashboard view models
  Services/                  Business logic layer (see section 5 below)
  Pages/
    Account/                 Register, Login, Logout, Profile, AccessDenied
    Menu/                    Public menu browsing + add-to-cart
    Reservations/            Customer: list, create (availability search), edit
    Orders/                  Customer: cart/checkout, order history, order details/receipt
    Feedback/                Customer: leave feedback, feedback history
    Notifications/           Customer: notification inbox
    Admin/                   Dashboard, Venue, MenuManagement, Promotions, Users,
                              Monitoring, Reports, Announcements, Settings
    Staff/                   Dashboard, Reservations, Orders, Billing
    Shared/                  _Layout (nav + chatbot widget), Error page
  wwwroot/css, wwwroot/js    Design system (site.css) and behaviour (site.js, chatbot.js,
                              dashboard-charts.js)
```

## 4. Roles & access control

Role-based authorization is wired in `Program.cs` using **Razor Pages folder conventions**
(`AuthorizeFolder`), not per-page attributes:

- `/Admin/**` → Administrator only
- `/Staff/**` → Staff or Administrator
- `/Reservations/**`, `/Orders/**`, `/Feedback/**`, `/Notifications/**`, `/Account/Profile`
  → any signed-in user
- everything else (Home, Menu, Login, Register) → public

An Administrator can promote/demote any account's role, and activate/deactivate accounts,
from **Admin → Users & Roles**.

## 5. Where each requirement lives

### Administrator module
| Brief requirement | Implementation |
|---|---|
| Secure login | ASP.NET Core Identity, `/Account/Login` |
| Manage dining areas, tables, hours, categories | `Admin/Venue`, `Admin/MenuManagement`, `Admin/Settings` |
| Manage customer/staff accounts & roles | `Admin/Users` |
| Manage menu items, pricing, availability, promos | `Admin/MenuManagement`, `Admin/Promotions` |
| Monitor reservations/orders/payments | `Admin/Monitoring` (tabbed) |
| Reports: reservations, sales, popular items, customer activity | `Admin/Reports` (filtered, exportable) |
| Announcements & promotional campaigns | `Admin/Announcements`, `Admin/Promotions` |

### Restaurant Staff module
| Brief requirement | Implementation |
|---|---|
| Login + manage own profile | `/Account/Login`, `/Account/Profile` (linked from the Staff menu) |
| View/manage daily reservations | `Staff/Reservations` (date filter) |
| Confirm/modify/cancel reservations | Confirm / Seat / Complete / No-show / Cancel actions on `Staff/Reservations` |
| Receive/update/process orders | `Staff/Orders` |
| Update order status (Preparing → Ready → Served → Completed) | One-click "advance status" button per order |
| Record payment status / billing | `Staff/Billing` |
| View special requests & reservation history | Shown inline on `Staff/Reservations`; full history via `Admin/Monitoring` |

### Customer module
| Brief requirement | Implementation |
|---|---|
| Register/login | `/Account/Register`, `/Account/Login` |
| Profile management | `/Account/Profile` |
| Browse digital menu | `/Menu` (search, category filter, vegetarian filter) |
| Make/modify/cancel reservations | `/Reservations` (Create/Edit/Index) |
| Place orders online | `/Menu` → cart → `/Orders/Cart` checkout |
| Reservation/order/payment history | `/Reservations`, `/Orders` |
| Track status in real time | Status badges on `/Orders`, `/Orders/Details`, `/Reservations` |
| Ratings & feedback | `/Feedback` |
| Notifications | In-app bell + `/Notifications`; optional email (see below) |

### Performance levels
- **Basic** - registration/login, reservation CRUD, profile, menu browsing/ordering,
  transaction records: fully implemented.
- **Intermediate** - automated availability checking (`ReservationService.GetAvailableTablesAsync`
  does real overlap detection against operating hours and existing bookings), live order-status
  updates, an instant confirmation/receipt page after booking or checkout, simulated online
  payment with status tracking, and Admin Reports with date-range filtering (daily/weekly/monthly
  presets): fully implemented.
- **Advanced** - role-based access control, table/reservation analytics (peak hours, table
  booking frequency), an interactive Chart.js dashboard, CSV/PDF/Excel export, in-app +
  optional email notifications, a rule-based "AI" chatbot, and an order-history-driven
  recommendation engine: all implemented (see notes below on the two simulated pieces).

## 6. Two things that are intentionally simulated

A coursework build can't safely ship real third-party credentials, so two features are built
as clearly-labelled simulations with a documented upgrade path:

- **Online payment** (`OrderService.PlaceOrderAsync`) - Card/Online orders are marked "Paid"
  immediately with a generated reference number; no real charge occurs. To go live, swap the
  block in `PlaceOrderAsync` for a real gateway call (Stripe, PayPal, etc.) and keep everything
  else (Payment entity, statuses, receipts) unchanged.
- **AI chatbot** (`Services/ChatbotService.cs`) - fully functional today using database-backed
  intents (opening hours, "my reservation/order" lookups) plus keyword-scored FAQ matching
  from the `ChatbotFaq` table - no API key required. `IChatbotService.GetAnswerAsync` is the
  single seam to swap in a real LLM call later without touching any page or the `/api/chatbot/ask`
  endpoint in `Program.cs`.

Everything else (availability engine, reports, recommendations, RBAC, exports, notifications)
is fully functional, real logic against the database - nothing else is mocked.

## 7. Optional: enabling real email notifications

Notifications always appear in-app. To also send email, edit `appsettings.json`:
```json
"SmtpSettings": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "UseSsl": true,
  "Username": "your-account@gmail.com",
  "Password": "an-app-password",
  "FromName": "RROMS Restaurant",
  "FromAddress": "your-account@gmail.com"
}
```
Email sending is best-effort and wrapped in try/catch (`NotificationService.TrySendEmailAsync`),
so a misconfigured or absent mail server never breaks a reservation or order.

## 8. Design notes

The UI uses a custom "Ember & Thyme" identity (warm ink, saffron-gold, herb-green) defined as
CSS variables in `wwwroot/css/site.css`, with Fraunces for display type and Inter for UI/body
text, rather than default Bootstrap styling. Admin/Staff screens favour dense, functional
Bootstrap tables and modals; customer-facing screens (Home, Menu) carry more of the brand
styling.

## 9. Suggested extension ideas (not required by the brief, but easy next steps)

- Wire a real payment gateway SDK into `OrderService.PlaceOrderAsync`.
- Add a background job (e.g. `IHostedService`) to auto-mark past `Confirmed` reservations as
  `NoShow` if no order/seating activity occurred.
- Expand `Staff/Reservations` with a full modify-details flow (currently staff confirm, seat,
  complete, mark no-show, or cancel; changing the date/time/party size is customer- or
  Admin-driven today via the same `IReservationService.UpdateReservationAsync`).
- Swap `ChatbotService` to call a hosted LLM for open-ended questions.
