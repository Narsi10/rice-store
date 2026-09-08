# Rice Store - Microservices Platform

An online store selling all types of rice, built with **.NET (ASP.NET Core Web API)**, **Angular**, and **MSSQL**, using a **microservices architecture**.

## Architecture Overview

```
                          +-----------------+
                          |  Angular SPA     |
                          |  (Web Browser)   |
                          +--------+---------+
                                   | HTTPS/REST
                                   v
                          +-----------------+
                          |   API Gateway    |  (YARP)
                          |  Routing / Auth  |
                          +--------+---------+
                                   |
        +--------------+-----------+-----------+--------------+
        v              v           v           v              v
  +----------+  +----------+ +----------+ +----------+ +----------+
  | Identity |  | Catalog  | |  Cart    | |  Order   | | Payment  |
  | Service  |  | Service  | | Service  | | Service  | | Service  |
  +----+-----+  +----+-----+ +----+-----+ +----+-----+ +----+-----+
       |             |            |            |            |
   +---v--+      +---v--+     +---v--+     +---v--+     +---v--+
   | MSSQL|      | MSSQL|     |Redis |     | MSSQL|     | MSSQL|
   +------+      +------+     +------+     +------+     +------+

     Inventory Service (MSSQL) + Notification Service
     All services communicate asynchronously via RabbitMQ
```

## Services

| Service | Port | Responsibility | Storage |
|---|---|---|---|
| **API Gateway** | 8000 | Single entry point, routing, auth forwarding | - |
| **Identity** | 8001 | Registration, login, JWT issuance, roles | MSSQL |
| **Catalog** | 8002 | Rice products, categories, brands, variants | MSSQL |
| **Cart** | 8003 | Shopping cart per user | Redis |
| **Inventory** | 8004 | Stock levels, reservation | MSSQL |
| **Order** | 8005 | Order creation & lifecycle (saga orchestrator) | MSSQL |
| **Payment** | 8006 | Payment processing, refunds | MSSQL |
| **Notification** | 8007 | Email/SMS on events | - |

## How services are interlinked

**Synchronous (REST via Gateway):** The Angular app never calls services directly. It calls the API Gateway, which routes to the correct service. Auth tokens (JWT) issued by Identity are validated at every service.

**Asynchronous (Events via RabbitMQ / MassTransit):** Services publish integration events and react to them. This decouples the checkout workflow:

```
Cart --(checkout)--> Order Service
   Order  publishes  OrderPlaced
   Inventory reacts, reserves stock, publishes StockReserved (or StockRejected)
   Payment reacts,  charges,       publishes PaymentCompleted (or PaymentFailed)
   Order  reacts,   confirms order, publishes OrderConfirmed
   Notification reacts, sends confirmation email
```

Failures trigger compensating events (Saga pattern) to keep data consistent
across independent databases.

## Running locally

```bash
docker compose up --build
```

Then browse:
- Gateway:  http://localhost:8000
- Swagger per service on its own port (e.g. Catalog: http://localhost:8002/swagger)

## Repository layout

```
rice-store/
  frontend/                 Angular SPA
  gateway/ApiGateway/        YARP reverse proxy
  shared/BuildingBlocks/     Shared contracts, integration events, base classes
  services/
    Identity/
    Catalog/
    Cart/
    Inventory/
    Order/
    Payment/
    Notification/
  docker-compose.yml
```
