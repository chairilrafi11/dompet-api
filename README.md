# dompet-api

Backend untuk aplikasi manajemen dompet. .NET 8 Web API + EF Core + ASP.NET Core Identity + JWT Bearer, terkoneksi ke SQL Server.

## Prasyarat

- .NET 8 SDK
- SQL Server 2022 (via Docker, port 1433) — lihat [docker-compose](../docker-compose.yml) di folder `playground`
- `dotnet-ef` tool (untuk migrasi):
  ```bash
  dotnet tool install --global dotnet-ef --version 8.0.11
  ```
  > Catatan: pastikan `DOTNET_ROOT="$HOME/.dotnet"` dan `$HOME/.dotnet/tools` ada di `PATH` (lihat bagian Troubleshooting).

## Konfigurasi

Connection string ada di `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "Default": "Server=localhost,1433;Database=Dompet;User Id=sa;Password=Your_Str0ng_P@ss!;TrustServerCertificate=True;"
}
```

Konfigurasi JWT juga di file yang sama (section `Jwt` — `Key`, `Issuer`, `Audience`, `ExpiryDays`).

Di produksi (Azure), semua ini di-override lewat **app settings** (environment variable), bukan file.

## Menjalankan

```bash
dotnet run          # http://localhost:8020
```

## Seed data demo

```bash
dotnet run -- seed
```

Isi: akun `demo@dompet.app` / `Demo123!`, 3 wallet, 8 kategori, 102 transaksi (6 bulan). Idempotent — skip kalau user demo sudah ada.

## Database

Membuat & menerapkan migrasi:

```bash
dotnet ef migrations add <NamaMigrasi>
dotnet ef database update
```

Migration existing: `InitialCreate`, `ChangeTransactionDateToUtc`, `AddTransactionCompositeIndexes`.

## Test

```bash
dotnet test Dompet.sln
```

Test pakai `WebApplicationFactory` + SQLite in-memory (tanpa perlu SQL Server berjalan). Catatan: SQLite tidak mendukung beberapa translasi (agregat `decimal`, `DateTimeOffset`) — itu sebabnya sebagian agregasi/filter dijalankan di C#.

## Struktur

```
Controllers/         # Auth, Wallets, Categories, Transactions, Analytics
Data/                # AppDbContext, DbSeeder
Models/              # ApplicationUser, Wallet, Category, Transaction
DTOs/                # request/response (PagedResult<T>, dll)
Services/            # business logic + JWT
Middleware/          # ExceptionHandlingMiddleware (ProblemDetails global)
Migrations/          # EF Core migrations
Program.cs           # DI, Identity, JWT, CORS, middleware, seed command
Dompet.Api.Tests/    # xUnit integration + unit tests
```

## Endpoint

| Method | Path | Deskripsi |
|---|---|---|
| POST | `/api/auth/register` | Daftar akun, return JWT |
| POST | `/api/auth/login` | Login, return JWT |
| GET/POST/PUT/DELETE | `/api/wallets` | CRUD dompet |
| GET/POST/PUT/DELETE | `/api/categories` | CRUD kategori |
| GET/POST/PUT/DELETE | `/api/transactions` | CRUD transaksi |
| GET | `/api/analytics/summary` | Income/expense/net bulan berjalan |
| GET | `/api/analytics/by-category` | Breakdown pengeluaran per kategori |
| GET | `/api/analytics/monthly-trend` | Tren income/expense per bulan |

Semua endpoint kecuali auth butuh header `Authorization: Bearer <token>`.

### GET `/api/transactions`

Filter (query param, semua opsional): `dateFrom`, `dateTo`, `categoryId`, `walletId`, `type`.
Paging: `page` (default 1), `pageSize` (default 20).

Response (camelCase):

```json
{
  "items": [ { "id": 1, "walletId": 2, "amount": 50000, "date": "2026-08-20T00:00:00Z", ... } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 102,
  "totalPages": 6
}
```

Create/update: field `date` opsional (`DateTime?`). Kalau tidak dikirim saat create → `UtcNow`; saat update → tanggal lama dipertahankan.

## Deployment (Azure)

Arsitektur target:

```
dompet-web  → Azure Static Web Apps   (custom domain + SSL, free)
dompet-api  → Azure App Service F1    (https://dompet-api-xxx.azurewebsites.net)
Dompet DB   → Azure SQL (free offer, 100k vCore-sec/bln)
```

### 1. Prasyarat

```bash
brew install azure-cli
az login
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.Web
```

### 2. Resource group + Azure SQL

```bash
az group create --name dompet-rg --location southeastasia
az sql server create --name dompet-sql --resource-group dompet-rg \
  --admin-user dompetadmin --admin-password 'STRONG_PASSWORD'
az sql db create --resource-group dompet-rg --server dompet-sql --name Dompet \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 1 --free-limit true
```

Firewall — izinkan service Azure + laptop dev:

```bash
az sql server firewall-rule create --resource-group dompet-rg --server dompet-sql \
  --name allowazure --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
MYIP=$(curl -4 -s ifconfig.me)
az sql server firewall-rule create --resource-group dompet-rg --server dompet-sql \
  --name myip --start-ip-address $MYIP --end-ip-address $MYIP
```

### 3. App Service + config

```bash
az appservice plan create --name dompet-plan --resource-group dompet-rg --sku F1 --is-linux
az webapp create --name dompet-api --resource-group dompet-rg --plan dompet-plan --runtime "DOTNETCORE:8.0"
```

App settings (secret wajib di sini, bukan di repo):

```bash
az webapp config appsettings set --name dompet-api --resource-group dompet-rg --settings \
  "ConnectionStrings__Default=Server=tcp:dompet-sql.database.windows.net,1433;Database=Dompet;User Id=dompetadmin;Password=STRONG_PASSWORD;Encrypt=True;" \
  "Jwt__Key=GANTI_DENGAN_KEY_RANDOM_32_CHAR_MIN" \
  "Jwt__Issuer=dompet-api" \
  "Jwt__Audience=dompet-web" \
  "Jwt__ExpiryDays=7"
```

> `Jwt__ExpiryDays` wajib ada — `JwtTokenService` membaca semua `Jwt:*`.

### 4. Publish & deploy API

```bash
dotnet publish Dompet.Api.csproj -c Release -o publish
cd publish && zip -r ../app.zip . && cd ..
az webapp deploy --resource-group dompet-rg --name dompet-api --src-path app.zip
```

### 5. Migrasi + seed ke DB Azure

```bash
export ConnectionStrings__Default='Server=tcp:dompet-sql.database.windows.net,1433;Database=Dompet;User Id=dompetadmin;Password=STRONG_PASSWORD;Encrypt=True;'
dotnet ef database update
dotnet run -- seed
```

### 6. CORS

Origin frontend dikonfigurasi di `Program.cs` (`AddCors` + `app.UseCors`). Tambah origin baru → ubah `Program.cs` → re-publish + re-deploy.

### 7. Frontend (Static Web Apps)

Di repo `dompet-web`: set `VITE_API_URL=https://dompet-api-xxx.azurewebsites.net/api` (wajib ada `/api`), lalu push — GitHub Actions (workflow SWA) otomatis build `dist` dan deploy.

### 8. Budget alert

Portal → Cost management + Billing → Budgets → atur `$5/bulan` + alert email (50/90/100%). Always-free seharusnya `$0`.

## Troubleshooting

- `dotnet ef` "You must install .NET" → tool versi salah / runtime tak ditemukan. Set di `~/.zshrc`:
  ```bash
  export PATH="$HOME/.dotnet:$PATH"
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$PATH:$HOME/.dotnet/tools"
  ```
- `dotnet test` error `MSB1011` (banyak project) → selalu `dotnet test Dompet.sln` atau sebut project-nya.
- Login 500 `ArgumentNullException int.Parse` → `Jwt__ExpiryDays` belum diset di app settings.
- Login 500 `Login failed for user` → password di connection string app settings salah.
- Request browser 404 di `/auth/login` → `VITE_API_URL` harus berakhir `/api`.
- 500 sesaat setelah deploy → app masih restart; tunggu lalu retry.
