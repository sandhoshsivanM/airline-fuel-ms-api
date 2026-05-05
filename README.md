# Airline Fuel Management System — Backend

ASP.NET Core 10 Web API for managing airlines (aircraft), fuel providers (with multi-country addresses), fuel transactions, and auto-generated invoices. Ships with JWT auth, paginated/sortable/filtered list endpoints, and a generic `ApplyFilter` helper.

> The Angular frontend (`AirlineFuelMS.Web/`) is a separate project and is **not** included in this repository.

---

## Stack

- .NET 10 · ASP.NET Core Web API
- Entity Framework Core 10
- **SQLite** for local dev (single `.db` file, no setup)
- **PostgreSQL** for hosted/production (auto-detected from `DATABASE_URL`)
- JWT Bearer auth · BCrypt password hashing · Swashbuckle for Swagger

---

## Quick start (local — SQLite)

```bash
# 1. Restore + build
dotnet build AirlineFuelMS.slnx

# 2. Run migrations + seed (the API does this automatically on startup)
dotnet run --project AirlineFuelMS.API --urls http://localhost:5080

# 3. Open Swagger
#    http://localhost:5080/swagger
```

The SQLite database file `AirlineFuelMS.API/AirlineFuel.db` is created on first run, seeded with the data below.

### Seeded credentials

| Username | Password    | Role        |
|---|---|---|
| `admin`  | `Admin@123` | Admin       |
| `user1`  | `User@123`  | NormalUser  |

Login → `POST /api/auth/login` → returns a JWT.

### Inspect / query the local SQLite DB

```bash
DB="AirlineFuelMS.API/AirlineFuel.db"
sqlite3 -header -column "$DB" "SELECT * FROM Airlines;"
sqlite3 -header -column "$DB" "
  SELECT fp.Code, fp.Name, c.Name AS Country, fpa.City, fpa.IsHeadOffice
  FROM FuelProviders fp
  JOIN FuelProviderAddresses fpa ON fpa.FuelProviderId = fp.Id
  JOIN Countries c               ON c.Id = fpa.CountryId
  ORDER BY fp.Id, fpa.IsHeadOffice DESC;
"
```

GUI alternatives: DB Browser for SQLite, DBeaver, TablePlus, VS Code "SQLite Viewer" extension.

---

## Deploy to Render

1. **Push this repo to GitHub** (already done if you cloned it).
2. Go to [render.com](https://render.com) → **New +** → **Blueprint**.
3. Connect your GitHub account → select this repo → Render detects `render.yaml`.
4. Click **Apply**. Render will:
   - Provision a free Postgres database (`airlinefuel-db`)
   - Build the Docker image and deploy the web service (`airlinefuel-api`)
   - Inject `DATABASE_URL` (the Postgres connection string) into the API
   - Generate a random `Jwt__Key` secret
5. After ~3–5 minutes the API is live at `https://airlinefuel-api.onrender.com`.
6. Visit `/swagger` to test it.

> **Free tier caveats**: the web service spins down after 15 min idle (cold start ~30s on first request). Free Postgres expires after 30 days, then $7/mo to keep.

### Schema management on Postgres

When `DATABASE_URL` is present, [`Program.cs`](AirlineFuelMS.API/Program.cs) calls `EnsureCreated()` instead of running the SQLite-specific migrations — Postgres syntax differs, so we recreate from the EF model. Local dev still uses migrations against SQLite. If you need versioned Postgres migrations for production schema evolution, generate a separate migration assembly — not done here to keep the demo simple.

---

## Connect to the hosted Postgres for SQL queries

After Render deploys the database:

1. Open the `airlinefuel-db` page on the Render dashboard.
2. Find **External Database URL** — it looks like `postgresql://airlinefuel:somepassword@dpg-xxx.oregon-postgres.render.com/airlinefuel`.
3. Use any Postgres client to connect:

   **psql** (CLI):
   ```bash
   # macOS:  brew install libpq && brew link --force libpq
   psql 'postgresql://airlinefuel:somepassword@dpg-xxx.oregon-postgres.render.com/airlinefuel?sslmode=require'
   ```

   **GUI options**: pgAdmin, DBeaver, TablePlus, Postico — paste the External URL.

4. Sample queries (same data, different tool):
   ```sql
   SELECT id, code, name, model, "PassengerCapacity", "FuelTankCapacityLiters" FROM "Airlines";

   SELECT fp."Code", fp."Name", c."Name" AS country, fpa."City", fpa."IsHeadOffice"
   FROM "FuelProviders" fp
   JOIN "FuelProviderAddresses" fpa ON fpa."FuelProviderId" = fp."Id"
   JOIN "Countries"             c   ON c."Id" = fpa."CountryId"
   ORDER BY fp."Id", fpa."IsHeadOffice" DESC;
   ```
   (EF Core uses quoted PascalCase identifiers on Postgres by default.)

---

## Project layout

```
.
├── AirlineFuelMS.slnx                 # solution file
├── AirlineFuelMS.Core/                # entities, DTOs, attributes (no infra deps)
│   ├── Entities/                      # Airline, FuelProvider, FuelProviderAddress, etc.
│   ├── DTOs/                          # paginated query types, create/update/response DTOs
│   └── Attributes/SearchAttribute.cs  # marks searchable string/int properties
├── AirlineFuelMS.Infrastructure/      # EF Core, services, generic ApplyFilter helper
│   ├── Data/AppDbContext.cs
│   ├── Data/SeedData.cs
│   ├── Migrations/                    # SQLite migrations (Postgres uses EnsureCreated)
│   ├── Services/                      # AirlineService, FuelProviderService, etc.
│   └── Extensions/RepositoryExtensions.cs   # generic search + filter via reflection
├── AirlineFuelMS.API/                 # controllers, Program.cs, appsettings.json
│   ├── Controllers/
│   └── Program.cs                     # auto-detects SQLite vs Postgres
├── Dockerfile                         # multi-stage build for Render
├── render.yaml                        # Render Blueprint (web service + Postgres)
└── README.md
```

---

## API at a glance

All list endpoints accept the same query envelope:

```
?page=1&pageSize=20&sortBy=name&sortDirection=desc&search=term
```

Plus per-entity filters:

| Endpoint                              | Filters                                              |
|---|---|
| `GET /api/airlines`                   | `country`, `isActive`                                |
| `GET /api/fuelproviders`              | `countryId`, `isActive`                              |
| `GET /api/fuelproviders/countries`    | (lookup — id, name, code)                            |
| `GET /api/fuelproviders/{id}/addresses` | (sub-resource)                                     |
| `GET /api/fueltransactions`           | `airlineId`, `fuelProviderId`, `status`, `fromDate`, `toDate` |
| `GET /api/invoices`                   | `airlineId`, `fuelProviderId`, `status`, `fromDate`, `toDate` |

Responses wrap items in:
```json
{ "items": [...], "totalCount": N, "page": 1, "pageSize": 20, "totalPages": 1, "hasNextPage": false, "hasPreviousPage": false }
```

Admin-only writes (`POST` / `PUT` / `DELETE`) require `role=Admin` in the JWT.

---

## License

For learning / demo purposes.
