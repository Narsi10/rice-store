# Rice Store - How the Services Interlink

This document explains what talks to what, and why.

## Two communication styles

The platform uses two distinct communication styles, on purpose:

1. **Synchronous REST (via the API Gateway)** for user-facing reads/writes that
   need an immediate answer (browse products, view cart, place order).
2. **Asynchronous events (via RabbitMQ / MassTransit)** for the checkout
   workflow that spans multiple services and must stay consistent even if one
   service is briefly down.

## Synchronous paths (through the Gateway)

The Angular app only ever calls the gateway at `http://localhost:8000`.
YARP routes by URL prefix:

| Frontend calls           | Gateway routes to | Service   | Storage |
|--------------------------|-------------------|-----------|---------|
| `/api/auth/*`            | identity cluster  | Identity  | MSSQL   |
| `/api/products/*`        | catalog cluster   | Catalog   | MSSQL   |
| `/api/cart/*`            | cart cluster      | Cart      | Redis   |
| `/api/stock/*`           | inventory cluster | Inventory | MSSQL   |
| `/api/orders/*`          | order cluster     | Order     | MSSQL   |
| `/api/payments/*`        | payment cluster   | Payment   | MSSQL   |

Each service owns its own database. No service reads another service's tables.

## Asynchronous path: the checkout saga

When a customer checks out, the Order service starts a **choreography saga**.
Each service reacts to events and emits new ones. The message contracts live in
`shared/BuildingBlocks/IntegrationEvents` so producers and consumers agree on
shape.

```
Angular --POST /api/orders--> [Order]
    [Order]      saves Pending order, publishes  OrderPlaced ─┐
                                                              v
    [Inventory]  reserves stock ── ok ──> publishes StockReserved ─┐
                              └─ no stock ─> publishes StockRejected │
                                                              v      │
    [Payment]    charges card ── ok ──> publishes PaymentCompleted   │
                              └─ fail ──> publishes PaymentFailed     │
                                                              v      │
    [Order]      PaymentCompleted -> status Confirmed, publishes OrderConfirmed
                 StockRejected/PaymentFailed -> status Cancelled, publishes OrderCancelled
                                                              v
    [Notification] OrderConfirmed  -> "order confirmed" email
                   OrderCancelled  -> "order cancelled" email
```

### Who publishes / consumes what

| Event            | Published by | Consumed by            |
|------------------|--------------|------------------------|
| `OrderPlaced`    | Order        | Inventory              |
| `StockReserved`  | Inventory    | Order, Payment         |
| `StockRejected`  | Inventory    | Order                  |
| `PaymentCompleted` | Payment    | Order                  |
| `PaymentFailed`  | Payment      | Order                  |
| `OrderConfirmed` | Order        | Notification           |
| `OrderCancelled` | Order        | Notification, Inventory |

### Why events instead of direct calls

- **Resilience:** if Payment is briefly down, the `StockReserved` message waits
  in RabbitMQ and is processed when it recovers. No lost orders.
- **Loose coupling:** Order doesn't know Payment's URL or schema. It only knows
  the event contract.
- **Compensation:** failures publish compensating events (`StockRejected`,
  `PaymentFailed`) that walk the order back to `Cancelled` — the saga pattern,
  since we can't use a single ACID transaction across separate databases.

## Data ownership summary

| Service   | Owns                          | Never touches               |
|-----------|-------------------------------|-----------------------------|
| Identity  | Users, roles, password hashes | Any other DB                |
| Catalog   | Products                      | Orders, stock               |
| Cart      | Carts (Redis, transient)      | Orders                      |
| Inventory | Stock levels & reservations   | Products (uses ProductId)   |
| Order     | Orders, order items           | Stock, payments (via events)|
| Payment   | Payment records               | Orders (uses OrderId)       |

## Correlation & tracing

Every event carries a `CorrelationId` (set to the OrderId at checkout). This
lets you trace a single purchase across all services in logs — essential for
debugging distributed flows.

## Local run order

`docker compose up --build` starts infrastructure first (SQL Server, Redis,
RabbitMQ with health checks), then services wait for them via `depends_on`,
then the gateway comes up last.
