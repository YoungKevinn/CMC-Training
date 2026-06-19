# Homework Submission - Day 3

**Họ tên:** Đỗ Minh Khoa
**MSSV:** 2387700031
**Ngôn ngữ:** C# / ASP.NET Core 8 (không dùng Go)

---

## Các bài đã hoàn thành

- [x] Bài 1: Migrate sang Database (SQLite + EF Core)
- [x] Bài 2: Mở rộng Scan API (4 scan types mới: ip, port, ssl, tech)
- [x] Bài 3: Viết Unit Tests (xUnit + Moq)
- [x] Bài 4: Tích hợp Frontend (vanilla JS dashboard)
- [x] Bài 5: CI/CD với GitHub Actions (Bonus)
- [x] Bài 6: Deploy với Docker Compose (Bonus)
- [x] Bài 7: Tính năng EASM mới — Export Reports (Bonus)
- [ ] Bài 8: Deploy lên Cloud VM (Bonus)
- [ ] Bài 9: Domain & TLS/HTTPS (Bonus)
- [ ] Bài 10: Auto Deploy on Merge (Bonus)

---

## Link Repository

https://github.com/YoungKevinn/CMC-Training

## Pull Request

https://github.com/YoungKevinn/CMC-Training/pull/1

---

## Bài 1: Migrate sang Database

**Database chọn:** MySQL 8 (local instance, port 3307, user `root`/`root`)

**Cách implement:**
- `Pomelo.EntityFrameworkCore.MySql` 8.0.2 làm EF Core provider
- `AppDbContext` (EF Core 8) với 3 bảng: `Assets`, `ScanJobs`, `ScanResults`
- `EfAssetStorage` implements `IAssetStorage` — thay thế `MemoryStorage` từ Day 1
- Schema tự tạo lúc start server (`db.Database.EnsureCreated()`)
- Connection string trong `appsettings.json`: `Server=localhost;Port=3307;Database=mini_asm;User=root;Password=root;`
- Các cột có index (`Type`, `Status`, `ScanType`, ...) được `HasMaxLength` rõ ràng — MySQL không cho index cột `longtext` (default mapping của EF Core cho string không giới hạn)

**Minh chứng — API response khi tạo asset (ghi vào MySQL):**

```
POST http://localhost:8080/assets
Body: {"name":"mysql-test.com","type":"domain"}

Response 201:
{
  "id": "7d02d100-f092-4884-a69f-8b9f61c05454",
  "name": "mysql-test.com",
  "type": "domain",
  "status": "active",
  "created_at": "2026-06-19T06:20:09.620226Z",
  "updated_at": "2026-06-19T06:20:09.6202425Z"
}
```

**Minh chứng — Query trực tiếp vào MySQL bằng `mysql` CLI (xác nhận data thật trong DB):**

```
$ mysql -h 127.0.0.1 -P 3307 -u root -proot -e "SELECT id, name, type, status FROM mini_asm.Assets;"

id                                      name             type     status
7d02d100-f092-4884-a69f-8b9f61c05454    mysql-test.com   domain   active
```

**Minh chứng — Restart server, data vẫn còn (persistence):**

```
# Sau khi Stop-Process server và chạy lại dotnet run:
GET http://localhost:8080/assets

Response 200:
{
  "items": [
    {
      "id": "7d02d100-f092-4884-a69f-8b9f61c05454",
      "name": "mysql-test.com",
      "type": "domain",
      "status": "active",
      ...
    }
  ],
  "total": 1
}
```

**Minh chứng — ScanJobs table cũng hoạt động qua MySQL:**

```
$ mysql -h 127.0.0.1 -P 3307 -u root -proot \
  -e "SELECT Id, AssetId, ScanType, Status FROM mini_asm.ScanJobs;"

Id                                      AssetId                                  ScanType   Status
ee4c07da-e47d-4a92-8963-e4141d880cab    7d02d100-f092-4884-a69f-8b9f61c05454     dns        running
```

---

## Bài 2: Mở rộng Scan API

**Scan types đã implement:**

| Type | Mô tả | Asset |
|------|-------|-------|
| `dns` | DNS lookup A/AAAA/MX/NS/TXT | domain |
| `whois` | WHOIS via TCP port 43 | domain |
| `subdomain` | Subdomain enum qua crt.sh | domain |
| `cert_trans` | Cert transparency qua crt.sh | domain |
| `asn` | ASN lookup qua ip-api.com | ip |
| `all` | Chạy tất cả passive scans | domain |
| `ip` ⭐ | Geolocation + ASN + reverse DNS | ip |
| `port` ⭐ | TCP port scan (localhost only) | ip |
| `ssl` ⭐ | TLS cert inspection | domain/service |
| `tech` ⭐ | Technology detection từ HTTP headers | domain/service |

⭐ = Scan mới thêm trong Day 3

**Minh chứng — DNS scan google.com (completed, 1 result):**

```
POST /assets/c57e18aa.../scan  {"scan_type":"dns"}
→ {"id":"6b644d3b-...","scan_type":"dns","status":"pending",...}

GET /scan-jobs/6b644d3b-.../
→ {"status":"completed","results":1,"ended_at":"2026-06-19T03:46:49.310742"}

GET /scan-jobs/6b644d3b-.../results
→ {
    "domain": "google.com",
    "records": {
      "a":   ["142.250.199.78"],
      "aaaa": ["2404:6800:4005:805::200e"],
      "mx":  [{"exchange":"smtp.google.com.","preference":10}],
      "ns":  ["ns1.google.com.","ns2.google.com.","ns3.google.com.","ns4.google.com."],
      "txt": ["v=spf1 include:_spf.google.com ~all", ...]
    }
  }
```

**Minh chứng — SSL scan google.com (grade A+, TLS 1.3):**

```
POST /assets/c57e18aa.../scan  {"scan_type":"ssl"}
→ status: completed

GET /scan-jobs/e3cdbac0-.../results
→ {
    "domain": "google.com",
    "grade": "A+",
    "issues": [],
    "certificate": {
      "subject": "CN=*.google.com",
      "issuer": "CN=WE2, O=Google Trust Services, C=US",
      "days_until_expiry": 59,
      "is_expired": false
    },
    "connection": {
      "tls_version": "TLS 1.3",
      "cipher_suite": "TLS_AES_256_GCM_SHA384"
    }
  }
```

**Minh chứng — Port scan 127.0.0.1 (localhost, 25 ports, completed):**

```
POST /assets/faa848af.../scan  {"scan_type":"port"}
→ status: completed, scan_duration_ms: 805

GET /scan-jobs/30f874fe-.../results
→ {
    "ip_address": "127.0.0.1",
    "total_scanned": 25,
    "open_ports": [135, 445, 8080, 8888]
  }
```

---

## Bài 3: Viết Unit Tests

**File tests:**
- `Models/AssetValidationTests.cs` — 8 test cases cho model validation
- `Scanners/DnsScannerTests.cs` — test DNS scanner thật (real DNS lookup)
- `Scanners/SslScannerTests.cs` — test SSL scanner thật (real TLS handshake)
- `Scanners/PortScannerTests.cs` — test port scanner + `IsPrivateOrLocalhost` safety check
- `Services/AssetServiceTests.cs` — 14 test cases với Moq mock (Bonus)

**Minh chứng — `dotnet test` output:**

```
$ cd AssetManager.Tests
$ dotnet test

Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51, Duration: 4 s
         - AssetManager.Tests.dll (net8.0)
```

---

## Bài 4: Tích hợp Frontend

**File:** `frontend/index.html` (single-file, không cần npm/build step)

**Features:**
- Dashboard stats (total, domain, ip, service count)
- Danh sách assets với badge type/status
- Tạo asset mới (form)
- Xóa asset
- Search realtime (debounce 300ms)
- Trigger scan với dropdown chọn scan type (filter theo asset type)
- Auto-poll status scan job mỗi 2 giây
- Hiển thị kết quả JSON

**CORS:** Đã cấu hình `AllowAll` trong `Program.cs`

**Minh chứng — Health check server (dùng frontend gọi):**

```
GET http://localhost:8080/health

Response 200:
{
  "status": "ok",
  "asset_count": 2,
  "timestamp": "2026-06-19T03:46:25.8075886Z"
}
```

**Cách mở frontend:**
1. Chạy `dotnet run` trong thư mục `AssetManager/`
2. Mở file `frontend/index.html` trực tiếp trong browser
3. Dashboard kết nối tới `http://localhost:8080` qua CORS

---

## Bài 5: CI/CD với GitHub Actions

**File:** `.github/workflows/ci.yml`

**Jobs:**

| Job | Mô tả |
|-----|-------|
| `build-and-test` | `dotnet build` + `dotnet test` + upload test results artifact |
| `security-nuget` | `dotnet list package --vulnerable` — check CVE trong NuGet packages |
| `security-secrets` | Gitleaks — phát hiện API keys/passwords trong code |
| `security-trivy` | Trivy filesystem scan — CRITICAL/HIGH vulnerabilities |

**Trigger:** push/PR vào `main`, `homework`, `homework-final`

**Minh chứng — Cấu trúc workflow:**

```yaml
# .github/workflows/ci.yml
on:
  push:
    branches: [main, homework, homework-final]
  pull_request:
    branches: [main]

jobs:
  build-and-test:    # dotnet build + dotnet test (51 tests)
  security-nuget:    # dotnet list package --vulnerable
  security-secrets:  # gitleaks/gitleaks-action@v2
  security-trivy:    # aquasecurity/trivy-action (CRITICAL,HIGH)
```

---

## Bài 6: Deploy với Docker Compose

**Files:**
- `Dockerfile` — multi-stage build (sdk:8.0 → aspnet:8.0)
- `docker-compose.yml` — 2 services: `db` (MySQL 8) + `backend` (API), volume cho MySQL data

**Cách chạy:**

```bash
cd "Module Dev/Day3"

# Build và start (db + backend)
docker compose up -d --build

# Kiểm tra
docker compose ps
# → mini-easm-db    running (healthy)   0.0.0.0:3307->3306/tcp
# → mini-easm-api    running (healthy)   0.0.0.0:8080->8080/tcp

# Health check
curl http://localhost:8080/health
# → {"status":"ok",...}

# Stop
docker compose down
```

**Minh chứng — docker-compose.yml:**

```yaml
services:
  db:
    image: mysql:8
    container_name: mini-easm-db
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: mini_asm
    ports:
      - "3307:3306"
    volumes:
      - mysql_data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-uroot", "-proot"]

  backend:
    build: .
    container_name: mini-easm-api
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Port=3306;Database=mini_asm;User=root;Password=root;
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

volumes:
  mysql_data:
```

**Lưu ý:** `backend` nói chuyện với `db` qua tên service Docker (`Server=db;Port=3306`), khác với connection string `localhost:3307` dùng khi chạy `dotnet run` trực tiếp trên máy host.

---

## Bài 7: Tính năng EASM mới — Export Reports

**Feature:** Export asset data và scan results ra CSV/JSON để phân tích offline.

**Endpoints mới (ExportController):**

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/export/assets.csv` | Tất cả assets dạng CSV |
| GET | `/export/assets.json` | Tất cả assets dạng JSON |
| GET | `/assets/{id}/export/results.json` | Scan results của asset (JSON, download) |
| GET | `/assets/{id}/export/results.csv` | Scan results của asset (CSV, download) |
| GET | `/scan-jobs/{id}/export/results.json` | Results của 1 job (JSON, download) |

**Minh chứng — Test export endpoints:**

```bash
# Export tất cả assets ra CSV
curl http://localhost:8080/export/assets.csv
# → id,name,type,status,created_at,updated_at
#   c57e18aa-...,google.com,domain,active,2026-06-19T03:46:32Z,...
#   faa848af-...,127.0.0.1,ip,active,2026-06-19T03:46:39Z,...

# Export scan results ra JSON (tự động download file)
curl -O http://localhost:8080/assets/c57e18aa-.../export/results.json
# → File: asset_c57e18aa-..._results.json

# Export scan results ra CSV
curl -O http://localhost:8080/assets/c57e18aa-.../export/results.csv
# → id,job_id,asset_id,scan_type,created_at,data_summary
#   ...,dns,...,"a:1 mx:1 ns:4"
#   ...,ssl,...,"grade:A+ tls:TLS 1.3"
```

**File:** `AssetManager/Controllers/ExportController.cs`
