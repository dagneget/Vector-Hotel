# VECTOR HOTEL - System Documentation

## 🏨 Overview
**Vector Hotel** is a premium Hotel Reservation System (HRS) built using **WPF (C#)** for the desktop client and **ASP.NET Core** for the REST API. It is designed to handle guest management, room inventory, complex billing (folios), and advanced data reporting with a heavy focus on Role-Based Access Control (RBAC) and data integrity.

---

## 📂 Project Structure

### 🖥️ Client Application (WPF)
- **`Views/`**: XAML definitions for the user interface (MainDashboard, ReservationsView, PaymentsView, etc.).
- **`ViewModels/`**: Logic for data binding and UI state (MVVM Pattern).
- **`Models/`**: Client-side data structures mirroring the API entities.
- **`Services/`**: Communication layer (AuthService, ApiService, AuditService, DataStore).
- **`Assets/`**: Global styles, themes (Dark/Luminous), and resource dictionaries.
- **`Converters/`**: Value converters for XAML (e.g., BooleanToVisibility).

### ⚙️ Backend API (ASP.NET Core)
- **`Controllers/`**: RESTful endpoints for CRUD operations.
- **`Data/`**: `AppDbContext` and Repository patterns for SQL Server interaction.
- **`Models/`**: Server-side entities and DTOs.
- **`Services/`**: Business logic layer (Payment logic, early checkout penalties).
- **`Migrations/`**: Database schema versions and history.

---

## 👥 Actor Roles & Responsibilities (RBAC)

### 🔑 Administrator
- **Full Access**: Can manage all modules.
- **Staff Management**: Create, edit, and delete staff accounts.
- **System Settings**: Configure hotel name, tax rates, and contact details.
- **Audit Oversight**: Access to system-wide audit logs to track every user action.

### 🛎️ Receptionist
- **Guest Management**: Register new guests and maintain the customer database.
- **Reservations**: Create, update, and manage bookings.
- **Room Status**: Monitor room availability and handle Check-ins.
- **Restriction**: Cannot process payments, modify financial adjustments, or access system settings.

### 💰 Accountant (Finance)
- **Billing & Folios**: Full control over guest folios, processing payments, and verifying transactions.
- **Reports**: Generate and download financial statements and revenue logs.
- **Data Export**: Access to advanced data export for archival and reporting.
- **Restriction**: Cannot edit guest personal details or modify reservation dates.

---

## 🛠️ Module Functionalities

### 1. Dashboard
- Real-time occupancy analytics.
- "Recent Reservations" feed.
- Quick navigation to active modules.

### 2. Guest Management
- Comprehensive profile tracking (Name, Email, Phone, ID/Passport).
- Blacklist/Trust system integration.
- Searchable guest database.

### 3. Room Management
- Room inventory with Type categorization (Standard, Deluxe, Suite).
- Pricing tiers: Base Price, Weekend Price, and Holiday Price.
- Floor and Capacity management.

### 4. Reservations
- Date-based booking with automatic price calculation.
- Status workflow: `Pending` -> `CheckedIn` -> `CheckedOut`.
- Blacklist check: Prevents blacklisted guests from making new reservations.

### 5. Billing & Payments
- **Folio System**: Aggregates all charges (Room, Tax) and payments into a single ledger.
- **Validation**: Prevents payments exceeding the balance due.
- **Receipts**: Generates professional `.txt` receipts for every transaction.
- **Early Checkout**: Automatic penalty calculation (e.g., 1-night penalty).

### 6. Settings & Data
- **Global Branding**: Centralized hotel name and contact info.
- **Advanced Export**: Filtered data extraction (Rooms, Guests, Reservations) by category and date range.

---

## 🗄️ Database Design

### Relationships (Entity Relationship)
- **User 1:N AuditLogs**: Tracks who performed what action.
- **Customer 1:N Reservations**: A guest can have multiple stays.
- **RoomType 1:N Rooms**: Standardizes pricing and features for room groups.
- **Room 1:N Reservations**: Tracks booking history for each physical room.
- **Reservation 1:N Payments**: Handles partial or full settlements.
- **Reservation 1:N Charges**: Tracks room charges and custom adjustments.

### Key Tables
| Table | Description |
| :--- | :--- |
| `Users` | Authentication and Role assignment. |
| `Rooms` | Physical inventory and real-time status. |
| `Reservations` | Core transactional data (dates, room link, customer link). |
| `Payments` | Financial transactions with verification audit. |
| `SystemSettings` | Tax rates and metadata. |

---

## 🔄 System Workflows

### 📥 The Booking Workflow
1. **Receptionist** creates a new reservation.
2. System validates guest status (Not Blacklisted).
3. System checks room availability for the selected dates.
4. Total Price is calculated based on seasonal pricing rules.
5. Reservation is saved as `Pending`.

### 💳 The Payment Workflow
1. **Accountant** opens a guest Folio.
2. System displays all charges vs. payments received.
3. Accountant enters a payment (Cash/Card).
4. System validates the amount against the `Balance Due`.
5. Transaction is recorded, and an **Official Receipt** is generated.

### 📤 The Checkout Workflow
1. System checks the `Payment Status`.
2. If `Unpaid`, the system blocks the checkout action.
3. Once balance is `Settled`, the Room status is updated to `Available` (or `Cleaning`).
4. Audit Log records the successful departure.

---

*Last Updated: April 2026*
