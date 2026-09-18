# Smart Solar Microgrid Trading System (SSMTS) — Backend Web API

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-Atlas%20%2F%20Local-green.svg)](https://www.mongodb.com/)
[![Swagger](https://img.shields.io/badge/OpenAPI-Swagger%20UI-orange.svg)](http://localhost:5000/swagger)

> **Enterprise Application Development (SE4040) — Assignment 1 (2026)**  
> High-performance, modular RESTful Web API engineered with ASP.NET Core (.NET 8) and MongoDB for peer-to-microgrid renewable energy trading.

---

## 📖 1. System Overview

The **Smart Solar Microgrid Trading System (SSMTS)** is a decentralized energy management platform designed to balance solar energy supply and demand within localized smart microgrids.

### What the System Accomplishes:
* **For Prosumers (Solar Producers & Consumers):**
  * Sell surplus generated solar energy back to microgrid battery stations (**Export**).
  * Purchase electricity from microgrid stations during cloudy days or peak hours (**Import**).
  * Locate nearby stations via geospatial coordinates, reserve charging/discharging bays in advance, and receive dynamic, signed QR passes for energy physical transfers.
* **For Grid Station Operators:**
  * Manage local station charging bays and verify prosumer reservations using tamper-proof cryptographic QR tokens or 6-digit backup codes.
  * Record real-time meter readings to finalize physical energy transfers with automated valuation.
* **For Backoffice Administrators:**
  * Configure microgrid station nodes, dynamic pricing tiers (buy/sell rates), operating hours, and capacity bays.
  * Review real-time analytics across microgrids: energy volume turnover, financial settlements, user statuses, and active grid loads.

---

## 🏛️ 2. Architectural Design & Tech Stack

The API is built as a **Modular Fat Web API** adhering strictly to the separation of concerns:
```text
Controller (HTTP & Routing) 
    ↳ Service (Business Rules & Policies) 
        ↳ Repository (MongoDB Queries & Transactions) 
            ↳ MongoDB (Atlas / Replica Set)
```

* **Core Framework:** .NET 8 (C# 12)
* **Database & Driver:** MongoDB with official `MongoDB.Driver 3.12`
  * **Transactions:** Multi-document ACID transactions via `IClientSessionHandle` (stopping double-bookings atomically).
  * **Precision:** High-precision monetary and energy amounts stored strictly as BSON `Decimal128`.
  * **Geospatial:** MongoDB `2dsphere` indexes with GeoJSON Point coordinates for proximity queries.
* **Authentication & Security:** 
  * JWT Bearer authentication with role-based policies (`Backoffice`, `GridOperator`, `Prosumer`).
  * Password hashing via `BCrypt.Net-Next` (Work Factor 12).
  * Station node access scoping for Grid Operators (`BR-11`).
  * Rate limiting (Fixed window: 5 login attempts per minute per IP).
  * HMAC-SHA256 digital signatures for QR code generation and verification (`BR-12`).
* **Validation & Error Handling:**
  * FluentValidation pipeline validation.
  * RFC 9457 compliant `ProblemDetails` error responses with stable error codes and trace IDs.
* **Documentation & Exploration:** Swagger UI / OpenAPI specification.

---

## 🧩 3. Modules & Complete API Contract (41 Endpoints)

All REST endpoints reside under the `/api/v1` namespace (except `/health` which is a public probe):

### 🔑 Module 1: Identity and Accounts (15 Endpoints)
| Method | Route | Access | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Public (Rate-limited) | Sign-in for Prosumers and Staff; issues JWT token. |
| `POST` | `/api/v1/auth/register` | Public | Self-registration for new Prosumers. |
| `GET` | `/api/v1/auth/me` | Authenticated | Retrieve current user's profile and roles. |
| `GET` | `/api/v1/users` | Backoffice | List all staff members (Backoffice and Grid Operators). |
| `POST` | `/api/v1/users` | Backoffice | Create a new staff account. |
| `GET` | `/api/v1/users/{id}` | Backoffice | Get staff user details by ID. |
| `PUT` | `/api/v1/users/{id}` | Backoffice | Update staff user details. |
| `PATCH` | `/api/v1/users/{id}/status` | Backoffice | Activate or deactivate staff accounts. |
| `GET` | `/api/v1/prosumers` | Backoffice | List all registered prosumers. |
| `POST` | `/api/v1/prosumers` | Backoffice | Register a prosumer on their behalf. |
| `GET` | `/api/v1/prosumers/{nic}` | Backoffice, Owner | View prosumer details by NIC. |
| `PUT` | `/api/v1/prosumers/{nic}` | Backoffice, Owner | Update prosumer contact and profile info. |
| `DELETE` | `/api/v1/prosumers/{nic}` | Backoffice | Remove a prosumer record. |
| `PATCH` | `/api/v1/prosumers/{nic}/deactivate` | Owner, Backoffice | Deactivate account (blocked if active bookings exist). |
| `PATCH` | `/api/v1/prosumers/{nic}/reactivate` | Backoffice | Reactivate a deactivated prosumer account. |

### ⚡ Module 2: Nodes and Slots (10 Endpoints)
| Method | Route | Access | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/nodes` | Authenticated | List all stations (Prosumers see Active stations only). |
| `GET` | `/api/v1/nodes/nearby` | Prosumer | Geospatial search for nearby stations using latitude and longitude. |
| `GET` | `/api/v1/nodes/{id}` | Authenticated | View station details, pricing, and operating hours. |
| `POST` | `/api/v1/nodes` | Backoffice | Provision a new microgrid solar station. |
| `PUT` | `/api/v1/nodes/{id}` | Backoffice | Update station capacity, pricing, or operating hours. |
| `PATCH` | `/api/v1/nodes/{id}/deactivate` | Backoffice | Deactivate station (prohibited if active reservations exist). |
| `PATCH` | `/api/v1/nodes/{id}/activate` | Backoffice | Re-activate station. |
| `GET` | `/api/v1/nodes/{id}/slots` | Authenticated | View time slots and bay availability for a given date. |
| `POST` | `/api/v1/nodes/{id}/slots/generate` | Backoffice | Trigger manual slot generation across a date range. |
| `PATCH` | `/api/v1/slots/{id}` | Grid Operator, Backoffice | Update bay capacity or block an empty slot. |

### 📅 Module 3: Energy Reservations (9 Endpoints)
| Method | Route | Access | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/reservations` | Prosumer, Backoffice | Reserve an energy transfer bay in an available slot. |
| `GET` | `/api/v1/reservations` | Backoffice, Grid Operator | Filter reservations by station, status, or date range. |
| `GET` | `/api/v1/reservations/mine` | Prosumer | View own reservations history and upcoming bookings. |
| `GET` | `/api/v1/reservations/{id}` | Owner, Staff | Get specific booking details. |
| `PUT` | `/api/v1/reservations/{id}` | Owner, Backoffice | Modify requested energy or slot before cutoff deadline. |
| `PATCH` | `/api/v1/reservations/{id}/cancel` | Owner, Staff | Cancel booking and release bay back to the pool. |
| `PATCH` | `/api/v1/reservations/{id}/approve` | Grid Operator, Backoffice | Approve pending booking and generate signed QR code. |
| `PATCH` | `/api/v1/reservations/{id}/reject` | Grid Operator, Backoffice | Reject booking with reason and release bay. |
| `GET` | `/api/v1/dashboard/prosumer` | Prosumer | Personal dashboard: active bookings, net earnings, energy stats. |

### 📲 Module 4: QR, Transfers & Dashboards (7 Endpoints)
| Method | Route | Access | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/reservations/{id}/qr` | Owner | Retrieve signed HMAC-SHA256 QR payload and backup code. |
| `POST` | `/api/v1/transfers/verify` | Grid Operator | Scan and verify QR code, transitioning booking to InProgress. |
| `POST` | `/api/v1/transfers/{reservationId}/finalize` | Grid Operator | Input start/end meter readings to compute value and complete. |
| `GET` | `/api/v1/transfers` | Backoffice, Grid Operator | Audit log of all completed energy transfers. |
| `GET` | `/api/v1/dashboard/operator` | Grid Operator | Operational dashboard for assigned microgrid nodes. |
| `GET` | `/api/v1/dashboard/backoffice` | Backoffice | High-level financial and energy turnover analytics. |
| `GET` | `/health` | Public | System health check (MongoDB connectivity ping). |

---

## ⚖️ 4. Business Rules Enforced in Code

| Rule | Location | Description & Enforcement |
|---|---|---|
| **BR-01** | `ReservationPolicy.CanBook` | Slots must start in the future and within 7 days (`MaxAdvanceDays`). |
| **BR-02** | `ReservationPolicy.CanChange` | Modifications close 12 hours (`ChangeCutoffHours`) before slot starts. |
| **BR-03** | `ReservationPolicy.CanChange` | Cancellations close 12 hours before slot start time. |
| **BR-04** | `SlotRepository.TryHoldBayAsync` | Atomic conditional increment (`bookedCount < capacity`) + unique partial index `ux_slot_prosumer_active` prevents double booking. |
| **BR-05** | `ReservationPolicy.CheckEnergy` | Requested kWh must be between 0.5 kWh (`MinKwh`) and station maximum. |
| **BR-06** | `NodeService.DeactivateAsync` | Station deactivation blocked if active bookings exist whose slot has not ended. |
| **BR-07** | `ReservationService`, `SlotService` | Station must be Active and Slot must be Available. |
| **BR-08** | `SlotService.UpdateSlotAsync` | Only empty slots (`bookedCount == 0`) can be blocked; capacity cannot drop below bookings. |
| **BR-09** | `NicRules` | Sri Lankan NIC regex validation (`^([0-9]{9}[VvXx]\|[0-9]{12})$`). |
| **BR-10** | `ReservationService`, `ProsumerService` | Account active status re-read before write; self-deactivation blocked if active bookings exist. |
| **BR-11** | `NodeScopeHandler` | Grid Operators can only view or act on stations assigned to their JWT `nodeIds`. |
| **BR-12** | `QrService`, `TransferService` | Cryptographic signature validation, version check, time window check (opens 30m prior), single use. |
| **BR-13** | `ITradePricing`, `TransferService` | Unit price snapshot preserved; value = `actualKwh * unitPrice` (rounded to 2 decimal places). |
| **BR-14** | `ReservationService` | Slot bay released atomically in MongoDB transaction on cancellation or rejection. |
| **BR-15** | `ReservationExpiryWorker` | Background worker auto-expires pending bookings past start time and marks approved bookings as `NoShow` past end time. |

---

## ⚙️ 5. Getting Started & Setup Guide

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
* A running MongoDB instance (Local or [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)).

### Step-by-Step Installation

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/SmartSolarMicrogrid/backend.git
   cd backend
   ```

2. **Configure Database Connection:**
   Open `SmartSolarMicrogrid.API/appsettings.json` (or `appsettings.Development.json`) and configure your connection string:
   ```json
   "MongoDbSettings": {
     "ConnectionString": "mongodb+srv://<username>:<password>@yourcluster.mongodb.net/?retryWrites=true&w=majority",
     "DatabaseName": "SmartSolarMicrogrid"
   }
   ```

3. **Restore Dependencies & Build:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the API:**
   ```bash
   dotnet run --project SmartSolarMicrogrid.API
   ```
   *(Or navigate into the directory `cd SmartSolarMicrogrid.API` and run `dotnet run`)*

5. **Explore Swagger UI & Health Check:**
   * Swagger Documentation: [http://localhost:5000/swagger](http://localhost:5000/swagger)
   * System Health Probe: [http://localhost:5000/health](http://localhost:5000/health)

---

## 👤 6. Default Seeded Accounts

On initial startup, the database seeder automatically initializes the default Backoffice administrator if the database is empty:

* **Role:** `Backoffice`
* **Email:** `admin@solarmicrogrid.com`
* **Password:** `Admin@1234`

---

## 🧪 7. Quick Testing Walkthrough (Swagger / Postman)

1. **Authenticate:**
   * Call `POST /api/v1/auth/login` with `admin@solarmicrogrid.com` and `Admin@1234`.
   * Copy the returned JWT token, click **Authorize** in Swagger, and paste the token (`Bearer <token>` or `<token>`).
2. **Register a Prosumer:**
   * Call `POST /api/v1/auth/register` with NIC, name, email, phone, and password.
3. **Provision a Station Node:**
   * Call `POST /api/v1/nodes` (as Backoffice) with coordinates (e.g., Colombo: lat `6.9044`, lng `79.9542`) and pricing.
4. **Generate Slots:**
   * Call `POST /api/v1/nodes/{id}/slots/generate` for upcoming dates.
5. **Create & Finalize an Energy Transfer:**
   * Prosumer reserves a bay via `POST /api/v1/reservations`.
   * Operator approves via `PATCH /api/v1/reservations/{id}/approve`.
   * Prosumer retrieves QR via `GET /api/v1/reservations/{id}/qr`.
   * Operator verifies QR via `POST /api/v1/transfers/verify`.
   * Operator finalizes meter readings via `POST /api/v1/transfers/{reservationId}/finalize`.

---

## 📁 8. Project Structure

```text
backend/
├── .gitignore
├── README.md
├── SmartSolarMicrogrid.sln
└── SmartSolarMicrogrid.API/
    ├── Auth/                  # RoleConstants, JWT Generator, NodeScopeHandler
    ├── Common/                # ErrorCodes, ErrorCatalog, DomainException
    ├── Configuration/         # DependencyInjection, AppSettings options
    ├── Controllers/           # 41 REST Endpoints (Auth, Users, Prosumers, Nodes, Slots, Reservations, Transfers, Dashboard)
    ├── DTOs/                  # Request & Response Contracts per module
    ├── Data/                  # MongoDbContext, IndexInitializer, MongoTransactionRunner, Seeder
    ├── Middleware/            # ExceptionHandlingMiddleware (RFC 9457 ProblemDetails)
    ├── Models/                # Aggregate Models (User, Prosumer, SolarStationInfo, EnergyBookingSlot, EnergyReservation)
    ├── Repositories/          # MongoDB Repositories with atomic operations
    ├── Services/              # Core business services, pricing strategies, and rule policies
    ├── Utilities/             # ColomboTime helper, format rules
    ├── Validators/            # FluentValidation rules for requests
    ├── Workers/               # SlotGenerationWorker & ReservationExpiryWorker
    ├── appsettings.json
    └── Program.cs             # ASP.NET Core Composition Root
```
