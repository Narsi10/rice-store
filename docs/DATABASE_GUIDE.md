# Database Guide — Rice Store

This guide covers the database design for the whole application and how to
connect the services to SQL Server.

## Key principle: one database per service

In microservices, each service owns its **own** database. No service reads
another service's tables — they talk through APIs and events instead. So you
create **5 separate databases**:

| Database      | Owned by          | Holds                              |
|---------------|-------------------|------------------------------------|
| `IdentityDb`  | Identity service  | Users (customers + admins), roles  |
| `CatalogDb`   | Catalog service   | Rice products, price, cost, stock  |
| `OrderDb`     | Order service     | Orders + order items               |
| `InventoryDb` | Inventory service | Stock levels & reservations        |
| `PaymentDb`   | Payment service   | Payment records                    |

> There is no separate "users DB" and "admins DB". Both customers and admins
> live in the **same** `IdentityDb.Users` table, distinguished by the `Role`
> column (`Customer`, `Admin`, `Delivery`). That's the standard approach —
> one users table, roles decide permissions.

---

## Two ways to create the tables

**Option A — Let EF Core create them for you (easiest).**
Each service already calls `EnsureCreated()` / seeding on startup. If SQL Server
is reachable, the databases and tables are created automatically the first time
a service runs. You don't have to write any SQL. See "Connecting" below.

**Option B — Create them by hand with SQL (this guide).**
Use the scripts below if you want to build the schema yourself in SQL Server
Management Studio (SSMS) or Azure Data Studio. Run each script against a fresh
SQL Server instance.

---

## 1. IdentityDb (users + admins)

```sql
CREATE DATABASE IdentityDb;
GO
USE IdentityDb;
GO

CREATE TABLE Users (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Email         NVARCHAR(256)    NOT NULL,
    PasswordHash  NVARCHAR(MAX)    NOT NULL,
    FullName      NVARCHAR(200)    NOT NULL,
    Role          NVARCHAR(50)     NOT NULL DEFAULT 'Customer',  -- Customer | Admin | Delivery
    CreatedOnUtc  DATETIME2        NOT NULL,
    UpdatedOnUtc  DATETIME2        NULL
);
GO

-- Email must be unique (used for login).
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
GO
```

---

## 2. CatalogDb (products)

```sql
CREATE DATABASE CatalogDb;
GO
USE CatalogDb;
GO

CREATE TABLE Products (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name         NVARCHAR(200)    NOT NULL,
    Description  NVARCHAR(1000)   NULL,
    RiceType     INT              NOT NULL,          -- enum: 0=Basmati,1=Brown,2=Jasmine,3=SonaMasoori,4=Parboiled,5=Idli,6=Sticky,99=Other
    Brand        NVARCHAR(120)    NULL,
    Price        DECIMAL(18,2)    NOT NULL,          -- selling price (INR)
    CostPrice    DECIMAL(18,2)    NOT NULL,          -- buying/cost price (INR)
    Stock        INT              NOT NULL DEFAULT 0,-- units available
    WeightKg     INT              NOT NULL,          -- pack size
    ImageUrl     NVARCHAR(MAX)    NULL,
    IsActive     BIT              NOT NULL DEFAULT 1,
    CreatedOnUtc DATETIME2        NOT NULL,
    UpdatedOnUtc DATETIME2        NULL
);
GO
```

---

## 3. OrderDb (orders + items)

```sql
CREATE DATABASE OrderDb;
GO
USE OrderDb;
GO

CREATE TABLE Orders (
    Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    CustomerId        NVARCHAR(200)    NOT NULL,
    CustomerEmail     NVARCHAR(256)    NOT NULL,
    Status            INT              NOT NULL,     -- 0=Pending,1=StockReserved,2=Paid,3=Confirmed(awaiting approval),4=Cancelled,5=Approved,6=Rejected
    TotalAmount       DECIMAL(18,2)    NOT NULL,
    -- admin review
    ReviewedBy        NVARCHAR(256)    NULL,
    ReviewedOnUtc     DATETIME2        NULL,
    ReviewNote        NVARCHAR(MAX)    NULL,
    -- payment
    PaymentMethod     NVARCHAR(50)     NULL,         -- 'Credit Card' | 'Debit Card' | 'UPI'
    PaymentInstrument NVARCHAR(100)    NULL,         -- masked, e.g. '**** 4242' or 'name@upi'
    TransactionId     NVARCHAR(100)    NULL,
    CreatedOnUtc      DATETIME2        NOT NULL,
    UpdatedOnUtc      DATETIME2        NULL
);
GO

CREATE TABLE OrderItems (
    Id           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderId      UNIQUEIDENTIFIER NOT NULL,
    ProductId    UNIQUEIDENTIFIER NOT NULL,
    ProductName  NVARCHAR(200)    NOT NULL,
    Quantity     INT              NOT NULL,
    UnitPrice    DECIMAL(18,2)    NOT NULL,
    CreatedOnUtc DATETIME2        NOT NULL,
    UpdatedOnUtc DATETIME2        NULL,
    CONSTRAINT FK_OrderItems_Orders
        FOREIGN KEY (OrderId) REFERENCES Orders (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_OrderItems_OrderId ON OrderItems (OrderId);
CREATE INDEX IX_Orders_CustomerId  ON Orders (CustomerId);
GO
```

> Note: `LineTotal` (Quantity × UnitPrice) is **computed in code**, not stored,
> so there's no column for it.

---

## 4. InventoryDb (stock)

```sql
CREATE DATABASE InventoryDb;
GO
USE InventoryDb;
GO

CREATE TABLE Stock (
    Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ProductId         UNIQUEIDENTIFIER NOT NULL,
    QuantityAvailable INT              NOT NULL DEFAULT 0,
    QuantityReserved  INT              NOT NULL DEFAULT 0,
    CreatedOnUtc      DATETIME2        NOT NULL,
    UpdatedOnUtc      DATETIME2        NULL
);
GO

CREATE UNIQUE INDEX IX_Stock_ProductId ON Stock (ProductId);
GO
```

---

## 5. PaymentDb (payments)

```sql
CREATE DATABASE PaymentDb;
GO
USE PaymentDb;
GO

CREATE TABLE Payments (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    OrderId       UNIQUEIDENTIFIER NOT NULL,
    Amount        DECIMAL(18,2)    NOT NULL,
    Status        INT              NOT NULL,   -- 0=Succeeded, 1=Failed
    TransactionId NVARCHAR(100)    NOT NULL,
    CreatedOnUtc  DATETIME2        NOT NULL,
    UpdatedOnUtc  DATETIME2        NULL
);
GO

CREATE INDEX IX_Payments_OrderId ON Payments (OrderId);
GO
```

---

## How the databases relate (logical, not enforced by FK)

Because each DB is separate, cross-service links are **by id only** — there are
no cross-database foreign keys.

```
IdentityDb.Users.Id  ─────────►  OrderDb.Orders.CustomerId   (who placed it)
CatalogDb.Products.Id ────────►  OrderDb.OrderItems.ProductId (what was bought)
CatalogDb.Products.Id ────────►  InventoryDb.Stock.ProductId  (stock per product)
OrderDb.Orders.Id     ────────►  PaymentDb.Payments.OrderId   (payment for order)
```

The only real (enforced) foreign key is inside OrderDb:
`OrderItems.OrderId → Orders.Id`.

---

## Connecting the services to the database

Each service reads its connection string from `appsettings.json` (or an
environment variable, which wins over the file).

### Local SQL Server (default instance)

Edit each service's `appsettings.json`. Example for Catalog
(`services/Catalog/Catalog.API/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "CatalogDb": "Server=localhost;Database=CatalogDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Use the matching key name per service:

| Service   | Connection string key | Database    |
|-----------|-----------------------|-------------|
| Identity  | `IdentityDb`          | IdentityDb  |
| Catalog   | `CatalogDb`           | CatalogDb   |
| Order     | `OrderDb`             | OrderDb     |
| Inventory | `InventoryDb`         | InventoryDb |
| Payment   | `PaymentDb`           | PaymentDb   |

### SQL Server with username/password (SQL auth)

```json
{
  "ConnectionStrings": {
    "CatalogDb": "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### SQL Server in Docker (what docker-compose uses)

When running via `docker compose up`, the compose file already injects
connection strings pointing at the `sqlserver` container, e.g.:

```
Server=sqlserver,1433;Database=CatalogDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

Connection string parts explained:
- `Server` — host + port of SQL Server (`localhost`, `localhost,1433`, or `sqlserver` inside Docker)
- `Database` — the DB name for that service
- `Trusted_Connection=True` — use your Windows login (no user/pass)
- `User Id` / `Password` — SQL auth credentials (use these instead of Trusted_Connection)
- `TrustServerCertificate=True` — accept the dev SSL cert (fine for local)

---

## Do you even need to write SQL?

**No, if you use Option A.** Because every service calls `EnsureCreated()` at
startup, just:
1. Install SQL Server (or run the SQL Server Docker image).
2. Put a valid connection string in each `appsettings.json`.
3. Run each service — it creates its database and tables automatically, and
   Identity/Catalog also seed initial data (admin user, sample products).

Write the SQL by hand (Option B) only if you want full control of the schema or
must create it before the app runs.

### Recommended for production: EF Core Migrations

For a real project, prefer migrations over `EnsureCreated()`:

```bash
# from a service folder, e.g. services/Catalog/Catalog.API
dotnet tool install --global dotnet-ef      # once
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Migrations version your schema over time, so you can evolve tables safely
instead of dropping/recreating.

---

## Quick verification

After creating the DBs and starting a service, verify in SSMS:

```sql
SELECT name FROM sys.databases
WHERE name IN ('IdentityDb','CatalogDb','OrderDb','InventoryDb','PaymentDb');

USE CatalogDb;
SELECT COUNT(*) AS ProductCount FROM Products;   -- should be 6 after seeding
```
