# dompet-api

Backend untuk aplikasi manajemen dompet. .NET 8 Web API + EF Core + ASP.NET Core Identity + JWT Bearer, terkoneksi ke SQL Server.

## Prasyarat

- .NET 8 SDK
- SQL Server 2022 (via Docker, port 1433) — lihat [docker-compose](../docker-compose.yml) di folder `playground`
- `dotnet-ef` tool (untuk migrasi):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Konfigurasi

Connection string ada di `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "Default": "Server=localhost,1433;Database=Dompet;User Id=sa;Password=Your_Str0ng_P@ss!;TrustServerCertificate=True;"
}
```

Konfigurasi JWT juga di file yang sama (section `Jwt`).

## Menjalankan

```bash
dotnet run          # http://localhost:8020
```

## Database

Membuat & menerapkan migrasi:

```bash
dotnet ef migrations add <NamaMigrasi>
dotnet ef database update
```

## Test

```bash
dotnet test Dompet.sln
```

Test pakai `WebApplicationFactory` + SQLite in-memory (tanpa perlu SQL Server berjalan).

## Struktur

```
Dompet.Api/            # project utama
  Controllers/         # Auth, Wallets, Categories, Transactions, Analytics
  Data/                # AppDbContext
  Models/              # ApplicationUser, Wallet, Category, Transaction
  DTOs/                # request/response
  Services/            # business logic + JWT
  Migrations/          # EF Core migrations
  Program.cs           # DI, Identity, JWT, middleware
Dompet.Api.Tests/      # xUnit integration tests
```

## Endpoint

| Method | Path | Deskripsi |
|---|---|---|
| POST | `/api/auth/register` | Daftar akun, return JWT |
| POST | `/api/auth/login` | Login, return JWT |
| GET/POST/PUT/DELETE | `/api/wallets` | CRUD dompet |
| GET/POST/PUT/DELETE | `/api/categories` | CRUD kategori |
| GET/POST/PUT/DELETE | `/api/transactions` | CRUD transaksi (+ filter `dateFrom`, `dateTo`, `categoryId`, `walletId`, `type`) |
| GET | `/api/analytics/summary` | Income/expense/net bulan berjalan |
| GET | `/api/analytics/by-category` | Breakdown pengeluaran per kategori |
| GET | `/api/analytics/monthly-trend` | Tren income/expense per bulan |

Semua endpoint kecuali auth butuh header `Authorization: Bearer <token>`.
