# Hotel Reservation System (HRS)

Welcome to the **Hotel Reservation System (HRS)** documentation portal. This file details the core architecture, capabilities, and workflows present in the reservation software.

---

## 1. System Architecture
HRS operates on a split modern deployment model:
- **Client (WPF Desktop App)**: Rich, data-bound administrative front-end styled via Google Fonts (Inter) and custom responsive structures.
- **Backend (ASP.NET Core Web API)**: Stateless routing layer managing atomic storage constraints.
- **Persistent DB**: SQL Server tracked via EF Core.

---

## 2. Core Functional Modules

### A. Authentication & Permission Trees
Strict Role-Based Access Control (RBAC) separates staff permissions:
- **Administrator**: Full crud capability on staff logs, audit trails, and configurations.
- **Receptionist**: Room assignment algorithms, check-ins, and guest intake forms.
- **Accountant**: Invoice resolution controls.

### B. Smart Reservation Lifecycle
Reservations have decoupled parameters defining state:
- **Payment Status**: `Pending`, `Confirmed`.
- **Room Status**: `CheckedIn`, `CheckedOut`, `Cancelled`.

#### Logic Routing Mappings:
| Condition | Evaluated Room State |
| :--- | :--- |
| `CheckedIn` + Payment `Pending` | **Reserved** (Locked but inactive) |
| `CheckedIn` + Payment `Confirmed` | **Occupied** (Full usage) |
| `CheckedOut` / `Cancelled` | **Available** (Resets state) |

### C. Booking Time Constraints
- Automatic retroactive booking blocks. Check-in must operate starting the day of configuration or later.

---

## 3. Financial Controls
- Dynamic ledger computation utilizing line variables.
