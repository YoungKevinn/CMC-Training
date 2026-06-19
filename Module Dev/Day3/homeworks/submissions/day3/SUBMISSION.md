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

**Database chọn:** SQLite (không cần cài đặt thêm)

**Cách implement:**
- `AppDbContext` (EF Core 8) với 3 bảng: `Assets`, `ScanJobs`, `ScanResults`
- `EfAssetStorage` implements `IAssetStorage` — thay thế `MemoryStorage` từ Day 1
- Schema tự tạo lúc start server (`db.Database.EnsureCreated()`)
- File DB: `mini_asm.db` trong thư mục project

**Minh chứng — API response khi tạo asset (data lưu vào SQLite):**

```
POST http://localhost:8080/assets
Body: {"name":"google.com","type":"domain"}

Response 201:
{
  "id": "c57e18aa-7e65-4ccc-a57b-1a0119850280",
  "name": "google.com",
  "type": "domain",
  "status": "active",
  "created_at": "2026-06-19T03:46:32.6340776Z",
  "updated_at": "2026-06-19T03:46:32.6340991Z"
}
```

**Minh chứng — Stats sau khi tạo 2 assets (domain + ip):**

```
GET http://localhost:8080/assets/stats

Response 200:
{
  "total": 2,
  "by_type": { "domain": 1, "ip": 1 },
  "by_status": { "active": 2 }
}
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
- `docker-compose.yml` — service + volume cho SQLite DB

**Cách chạy:**

```bash
cd "Module Dev/Day3"

# Build và start
docker compose up -d --build

# Kiểm tra
docker compose ps
# → mini-easm-api   running   0.0.0.0:8080->8080/tcp

# Health check
curl http://localhost:8080/health
# → {"status":"ok",...}

# Stop
docker compose down
```

**Minh chứng — docker-compose.yml:**

```yaml
services:
  backend:
    build: .
    container_name: mini-easm-api
    ports:
      - "8080:8080"
    volumes:
      - db_data:/data          # SQLite DB persist qua restart
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s

volumes:
  db_data:
```

**SQLite path trong container:** `/data/mini_asm.db` (mount từ Docker volume)

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
