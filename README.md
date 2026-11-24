# StockFlow API

Inventory management REST API using C#, ASP.NET Core 8, EF Core and SQL Server.

## Features
- Register products with unique SKU codes.
- Record stock receipts and withdrawals.
- Reject withdrawals that would create negative inventory.
- Find low-stock products and inspect transaction history.
- SQL Server row-version concurrency protection.
- Swagger interactive API documentation.

## Run locally with Docker
Requires Docker Compose and a platform supported by the SQL Server container (x86-64 recommended).

1. Copy .env.example to .env and replace the example password with a strong local password. Do not commit .env.
2. Run:

```bash
docker compose up --build -d
```

3. Once the database and API are ready, open http://localhost:5080/swagger.

The service is intentionally bound to localhost. The database volume persists between restarts. Use docker compose down to stop the services; docker compose down -v also deletes stored data.

## Example
Create a product through POST /products:

```json
{"name":"Notebook","sku":"NB-001","reorderLevel":5}
```

Use its returned ID to post {"delta":20} to /products/{id}/movements. Negative deltas represent withdrawals.

## Integration test
With the Docker services running, execute:

```bash
python scripts/smoke.py
```

The test covers creation, receipts, withdrawals, invalid negative stock, transaction history and low-stock filtering. It creates a uniquely named test product each time.

## Architecture
Client → ASP.NET Core API → Entity Framework Core → SQL Server

## Limitations
Local educational application. Authentication, role-based authorization, migrations and full product CRUD are not yet implemented. Do not expose the unauthenticated service to the internet. EnsureCreated is used for demonstration database initialization. Build and integration test results have not yet been independently verified.

Developed with AI assistance as a learning portfolio project.
