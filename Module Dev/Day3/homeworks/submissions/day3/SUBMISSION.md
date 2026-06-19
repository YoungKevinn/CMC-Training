# Homework Submission - Day 3

**Họ tên:** Đỗ Minh Khoa
**MSSV:** 2387700031
**Ngôn ngữ:** C# / ASP.NET Core 8 (không dùng Go)

---

## Các bài đã hoàn thành

- [x] Bài 1: Migrate sang Database (MySQL + EF Core)
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

## Bài 1: Migrate sang Database (30đ)

**Database chọn:** MySQL 8 (local instance, port 3307, user `root`/`root`)

**Cách implement:**
- `Pomelo.EntityFrameworkCore.MySql` 8.0.2 làm EF Core provider
- `AppDbContext` (EF Core 8) với 3 bảng: `Assets`, `ScanJobs`, `ScanResults`
- `EfAssetStorage` implements `IAssetStorage` — thay thế `MemoryStorage` từ Day 1
- Schema tự tạo lúc start server (`db.Database.EnsureCreated()`), không cần migration tay
- Connection string trong `appsettings.json`: `Server=localhost;Port=3307;Database=mini_asm;User=root;Password=root;`
- Cột có index (`Type`, `Status`, `ScanType`...) được `HasMaxLength` rõ ràng vì MySQL không index được cột `longtext` (mapping mặc định của EF Core cho string không giới hạn)

**Minh chứng — tạo asset (ghi vào MySQL):**

```bash
curl -s -X POST http://localhost:8080/assets \
    -H "Content-Type: application/json" \
    -d '{"name":"google.com","type":"domain"}'

{"id":"bf700e06-1877-4493-8f2c-22642b97accd","name":"google.com","type":"domain",
 "status":"active","created_at":"2026-06-19T06:30:42.433685Z","updated_at":"2026-06-19T06:30:42.433734Z"}

curl -s -X POST http://localhost:8080/assets \
    -H "Content-Type: application/json" \
    -d '{"name":"127.0.0.1","type":"ip"}'

{"id":"6f40b1cd-d7f4-4c81-b6f1-d545d71dc9de","name":"127.0.0.1","type":"ip",
 "status":"active","created_at":"2026-06-19T06:30:42.9244986Z","updated_at":"2026-06-19T06:30:42.9244988Z"}
```

**Minh chứng — `docker compose ps` / stats sau khi tạo 2 assets:**

```bash
curl -s http://localhost:8080/assets/stats

{"total":2,"by_type":{"ip":1,"domain":1},"by_status":{"active":2}}
```

**Minh chứng — data persist qua restart server (before/after):**

```bash
# BEFORE restart
curl -s http://localhost:8080/assets | python3 -c "import sys,json; print('total:', json.load(sys.stdin)['total'])"
total: 2

pkill -f "dotnet.*AssetManager"   # stop server
# ... dotnet run lại ...

# AFTER restart
curl -s http://localhost:8080/assets

{"items":[
  {"id":"6f40b1cd-...","name":"127.0.0.1","type":"ip","status":"active", ...},
  {"id":"bf700e06-...","name":"google.com","type":"domain","status":"active", ...}
],"total":2,"page":1,"limit":20,"total_pages":1}
```

→ Data còn nguyên sau restart, xác nhận MySQL lưu bền vững thay cho in-memory.

**Minh chứng — query trực tiếp MySQL bằng CLI (data thật trong DB, không phải app tự bịa):**

```bash
mysql -h 127.0.0.1 -P 3307 -u root -proot -e "SELECT id, name, type, status FROM mini_asm.Assets;"

id                                      name             type     status
6f40b1cd-d7f4-4c81-b6f1-d545d71dc9de    127.0.0.1        ip       active
bf700e06-1877-4493-8f2c-22642b97accd    google.com       domain   active
```

---

## Bài 2: Mở rộng Scan API (25đ)

**Scan types đã implement:**

| Type | Mô tả | Asset | Trạng thái |
|------|-------|-------|------------|
| `dns` | DNS lookup A/AAAA/MX/NS/TXT | domain | Đã có từ trước |
| `whois` | WHOIS via TCP port 43 | domain | Đã có từ trước |
| `subdomain` | Subdomain enum qua crt.sh | domain | Đã có từ trước |
| `cert_trans` | Cert transparency qua crt.sh | domain | Đã có từ trước |
| `asn` | ASN lookup qua ip-api.com | ip | Đã có từ trước |
| `all` | Chạy tất cả passive scans | domain | Đã có từ trước |
| `ip` ⭐ | Geolocation + ASN + reverse DNS | ip | **Mới — Day 3** |
| `port` ⭐ | TCP port scan (localhost/private only) | ip | **Mới — Day 3** |
| `ssl` ⭐ | TLS cert inspection | domain/service | **Mới — Day 3** |
| `tech` ⭐ | Technology detection từ HTTP headers | domain/service | **Mới — Day 3** |

**Minh chứng — DNS scan trên google.com:**

```bash
curl -s -X POST http://localhost:8080/assets/bf700e06-1877-4493-8f2c-22642b97accd/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"dns"}'

{"id":"636159d6-da00-49fb-9a0a-75510c1ea44c","asset_id":"bf700e06-...","scan_type":"dns",
 "status":"pending", ...}

curl -s http://localhost:8080/scan-jobs/636159d6-da00-49fb-9a0a-75510c1ea44c

{"id":"636159d6-...","status":"completed","results":1,
 "ended_at":"2026-06-19T06:32:06.711263", ...}

curl -s http://localhost:8080/scan-jobs/636159d6-da00-49fb-9a0a-75510c1ea44c/results

{"job_id":"636159d6-...","scan_type":"dns","results":[{
  "domain":"google.com",
  "records":{
    "a":["142.251.12.101","142.251.12.113","142.251.12.139", ...],
    "aaaa":["2404:6800:4003:c20::66", ...],
    "mx":[{"exchange":"smtp.google.com.","preference":10}],
    "ns":["ns1.google.com.","ns3.google.com.","ns4.google.com.","ns2.google.com."],
    "txt":["v=spf1 include:_spf.google.com ~all", ...]
  },
  "scanned_at":"2026-06-19T06:32:06.6625782Z"
}]}
```

**Minh chứng — SSL scan ⭐ (grade A+, TLS 1.3):**

```bash
curl -s -X POST http://localhost:8080/assets/bf700e06-1877-4493-8f2c-22642b97accd/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"ssl"}'

{"id":"171e673e-ad70-4be8-b97d-5f7d653de253", ...,"status":"pending"}

curl -s http://localhost:8080/scan-jobs/171e673e-ad70-4be8-b97d-5f7d653de253/results

{"job_id":"171e673e-...","scan_type":"ssl","results":[{
  "domain":"google.com",
  "certificate":{
    "subject":"CN=*.google.com",
    "issuer":"CN=WR2, O=Google Trust Services, C=US",
    "days_until_expiry":59,
    "is_expired":false,
    "is_self_signed":false
  },
  "connection":{"tls_version":"TLS 1.3","cipher_suite":"TLS_AES_256_GCM_SHA384"},
  "grade":"A+",
  "issues":[]
}]}
```

**Minh chứng — Tech detection scan ⭐:**

```bash
curl -s -X POST http://localhost:8080/assets/bf700e06-1877-4493-8f2c-22642b97accd/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"tech"}'

curl -s http://localhost:8080/scan-jobs/8f66f7f6-3105-46ad-9a46-73424bf26dda/results

{"job_id":"8f66f7f6-...","scan_type":"tech","results":[{
  "domain":"google.com",
  "technologies":[{"name":"gws","category":"Web Server","version":null,"confidence":100}],
  "headers":{"server":"gws","x-frame-options":"SAMEORIGIN", ...},
  "status_code":200
}]}
```

**Minh chứng — Port scan ⭐ trên 127.0.0.1 (localhost, an toàn):**

```bash
curl -s -X POST http://localhost:8080/assets/6f40b1cd-d7f4-4c81-b6f1-d545d71dc9de/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"port"}'

curl -s http://localhost:8080/scan-jobs/18e8812c-7855-46b1-9d7c-dd00f18eef89/results

{"job_id":"18e8812c-...","scan_type":"port","results":[{
  "ip_address":"127.0.0.1",
  "open_ports":[
    {"port":135,"protocol":"tcp","state":"open","service":"msrpc"},
    {"port":445,"protocol":"tcp","state":"open","service":"smb"},
    {"port":8080,"protocol":"tcp","state":"open","service":"http-alt"},
    {"port":8888,"protocol":"tcp","state":"open","service":"http-alt"}
  ],
  "closed_ports":21,"total_scanned":25,"scan_duration_ms":822
}]}
```

**Minh chứng — Safety check: port scan từ chối public IP (yêu cầu bảo mật bắt buộc):**

```bash
curl -s -X POST http://localhost:8080/assets \
    -H "Content-Type: application/json" -d '{"name":"8.8.8.8","type":"ip"}'
{"id":"17f6d756-1cab-4268-a448-5c06c33a4059","name":"8.8.8.8","type":"ip", ...}

curl -s -X POST http://localhost:8080/assets/17f6d756-1cab-4268-a448-5c06c33a4059/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"port"}'
{"id":"96ec5e95-ccca-413a-9a07-2a9c1e31c786", ...,"status":"pending"}

curl -s http://localhost:8080/scan-jobs/96ec5e95-ccca-413a-9a07-2a9c1e31c786

{"id":"96ec5e95-...","status":"failed",
 "error":"Port scan is only allowed on localhost and private IP ranges (127.x, 10.x, 172.16-31.x, 192.168.x)",
 "results":0}
```

→ Port scan **đúng yêu cầu bảo mật**: tự động reject public IP (8.8.8.8), chỉ cho phép localhost/private range.

**Minh chứng — IP scan ⭐ trên private IP cũng từ chối đúng cách (ip-api.com không geolocate được private range):**

```bash
curl -s -X POST http://localhost:8080/assets/6f40b1cd-d7f4-4c81-b6f1-d545d71dc9de/scan \
    -H "Content-Type: application/json" -d '{"scan_type":"ip"}'

curl -s http://localhost:8080/scan-jobs/a664b370-ab2d-4324-ad54-81f83363d16c

{"id":"a664b370-...","status":"failed","error":"reserved range","results":0}
```

→ Error handling đúng: 127.0.0.1 là reserved/private range, không geolocate được — báo lỗi rõ ràng thay vì crash.

---

## Bài 3: Viết Unit Tests (20đ)

**File tests:**
- `Models/AssetValidationTests.cs` — 8 test cases cho model validation
- `Scanners/DnsScannerTests.cs` — test DNS scanner thật (real DNS lookup, không mock)
- `Scanners/SslScannerTests.cs` — test SSL scanner thật (real TLS handshake)
- `Scanners/PortScannerTests.cs` — test port scanner + `IsPrivateOrLocalhost` safety check
- `Services/AssetServiceTests.cs` — 14 test cases với Moq mock (Bonus 3.3)

**Minh chứng — `dotnet test` output:**

```bash
cd AssetManager.Tests
dotnet test

Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51, Duration: ~2 min
         - AssetManager.Tests.dll (net8.0)
```

---

## Bài 4: Tích hợp Frontend (20đ)

**File:** `frontend/index.html` (single-file, không cần npm/build step)

**Features:**
- Dashboard stats (total, domain, ip, service count)
- Danh sách assets với badge type/status
- Tạo asset mới (form), xóa asset
- Search realtime (debounce 300ms)
- Trigger scan với dropdown chọn scan type (filter theo asset type)
- Auto-poll status scan job mỗi 2 giây, hiển thị kết quả JSON

**CORS:** `AllowAll` policy trong `Program.cs`

**Minh chứng — health check (endpoint frontend gọi để biết server sống):**

```bash
curl -s http://localhost:8080/health
{"status":"ok","asset_count":3,"timestamp":"2026-06-19T06:38:14.130149Z"}
```

**Minh chứng — CORS preflight (OPTIONS request, browser sẽ gửi trước khi POST từ origin khác):**

```bash
curl -s -i -X OPTIONS http://localhost:8080/assets \
    -H "Origin: http://example.com" \
    -H "Access-Control-Request-Method: POST"

HTTP/1.1 204 No Content
Access-Control-Allow-Methods: POST
Access-Control-Allow-Origin: *
```

→ CORS cho phép origin bất kỳ gọi API — đúng yêu cầu để frontend (mở trực tiếp từ file hoặc port khác) gọi được backend.

**Cách mở frontend:**
1. Chạy `dotnet run` trong thư mục `AssetManager/`
2. Mở file `frontend/index.html` trực tiếp trong browser
3. Dashboard kết nối tới `http://localhost:8080` qua CORS

---

## Bài 5: CI/CD với GitHub Actions (25đ — Bonus)

**File:** `.github/workflows/ci.yml`

| Job | Mô tả |
|-----|-------|
| `build-and-test` | `dotnet build` + `dotnet test` + upload test results artifact |
| `security-nuget` | `dotnet list package --vulnerable` — check CVE trong NuGet packages |
| `security-secrets` | Gitleaks — phát hiện API keys/passwords trong code |
| `security-trivy` | Trivy filesystem scan — CRITICAL/HIGH vulnerabilities |

**Trigger:** push/PR vào `main`, `homework`, `homework-final`. Xem kết quả tại:
`https://github.com/YoungKevinn/CMC-Training/actions`

---

## Bài 6: Deploy với Docker Compose (15đ — Bonus)

**Files:** `Dockerfile` (multi-stage sdk:8.0 → aspnet:8.0) + `docker-compose.yml` (2 services: `db` MySQL 8 + `backend` API)

**Cách chạy:**

```bash
cd "Module Dev/Day3"
docker compose up -d --build

docker compose ps
# mini-easm-db    running (healthy)   0.0.0.0:3307->3306/tcp
# mini-easm-api   running (healthy)   0.0.0.0:8080->8080/tcp

curl http://localhost:8080/health
# {"status":"ok",...}
```

`backend` nói chuyện với `db` qua tên service Docker (`Server=db;Port=3306`), khác connection string `localhost:3307` dùng khi chạy `dotnet run` trực tiếp trên host.

---

## Bài 7: Tính năng EASM mới — Export Reports (15đ — Bonus)

**Feature:** Export asset data và scan results ra CSV/JSON để phân tích offline.

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/export/assets.csv` | Tất cả assets dạng CSV |
| GET | `/export/assets.json` | Tất cả assets dạng JSON |
| GET | `/assets/{id}/export/results.json` | Scan results của asset (JSON download) |
| GET | `/assets/{id}/export/results.csv` | Scan results của asset (CSV download) |
| GET | `/scan-jobs/{id}/export/results.json` | Results của 1 job (JSON download) |

**Minh chứng:**

```bash
curl -s http://localhost:8080/export/assets.csv

id,name,type,status,created_at,updated_at
17f6d756-1cab-4268-a448-5c06c33a4059,8.8.8.8,ip,active,2026-06-19T06:32:46.634041,...
6f40b1cd-d7f4-4c81-b6f1-d545d71dc9de,127.0.0.1,ip,active,2026-06-19T06:30:42.924498,...
bf700e06-1877-4493-8f2c-22642b97accd,google.com,domain,active,2026-06-19T06:30:42.433685,...

curl -s http://localhost:8080/assets/bf700e06-1877-4493-8f2c-22642b97accd/export/results.csv

id,job_id,asset_id,scan_type,created_at,data_summary
ed5bd8ad-...,8f66f7f6-...,bf700e06-...,tech,2026-06-19T06:32:07.728952,count:1
958776d1-...,171e673e-...,bf700e06-...,ssl,2026-06-19T06:32:06.973529,grade:A+ tls:TLS 1.3
42e73166-...,636159d6-...,bf700e06-...,dns,2026-06-19T06:32:06.678692,a:6 mx:1 ns:4
```

**File:** `AssetManager/Controllers/ExportController.cs`
