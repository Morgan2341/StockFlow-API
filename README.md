# StockFlow API

An educational inventory REST API built with ASP.NET Core 8, EF Core and SQL Server.

## Quick start
Install the .NET 8 SDK and start a SQL Server instance. Create an empty database named StockFlow or allow the development account to create it. Set these environment variables (use your own credentials; never commit secrets):

```bash
export ConnectionStrings__Inventory='Server=localhost,1433;Database=StockFlow;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True'
export InitializeDatabase=true
dotnet run --urls http://localhost:5080
```

Open http://localhost:5080/swagger to explore endpoints.

## Example workflow
```bash
curl -X POST http://localhost:5080/products -H 'Content-Type: application/json' -d '{"name":"Notebook","sku":"NB-001","reorderLevel":5}'
curl -X POST http://localhost:5080/products/1/movements -H 'Content-Type: application/json' -d '{"delta":20}'
curl http://localhost:5080/products
```

Use the returned product ID instead of 1 if necessary. Negative deltas represent stock withdrawals. SQL Server row-version concurrency control prevents silent lost updates. Stock changes and movement records are saved together.

## Status and limitations
Initial MVP; CI is configured for compilation, not verified here. No authentication, role authorization, migrations, Docker deployment or automated integration tests yet. Run locally only; do not expose this unauthenticated API publicly. Database initialization uses EnsureCreated for demonstration only.

Developed with AI assistance as a learning portfolio project.
