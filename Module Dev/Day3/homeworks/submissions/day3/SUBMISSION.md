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
- Cột có index (`Type`, `Status`, `ScanType`...) được `HasMaxLength` rõ ràng vì MySQL không index được cột `longtext`

**Minh chứng (terminal thật, WSL2):**

```
 youngkevinn@kevin   ~   main
╰─❯ curl -v http://localhost:8080/health
...
< HTTP/1.1 200 OK
{"status":"ok","asset_count":0,"timestamp":"2026-06-19T07:35:11.995215Z"}

 youngkevinn@kevin   ~   main
╰─❯ ./test.sh
================================================================
 BAI 1: Health check + tao asset (proof MySQL hoat dong)
================================================================
{"status":"ok","asset_count":0,"timestamp":"2026-06-19T07:38:47.0986295Z"}

--- Tao domain asset ---
{"id":"6f7c9893-2619-4855-bb73-125b7c7059e1","name":"google.com","type":"domain","status":"active","created_at":"2026-06-19T07:38:47.4743636Z","updated_at":"2026-06-19T07:38:47.4744659Z"}

--- Tao IP asset ---
{"id":"7e606e46-be07-4f37-9794-09744dfe2330","name":"127.0.0.1","type":"ip","status":"active","created_at":"2026-06-19T07:38:47.7620477Z","updated_at":"2026-06-19T07:38:47.7620479Z"}

--- Stats sau khi tao 2 assets ---
{"total":2,"by_type":{"domain":1,"ip":1},"by_status":{"active":2}}
```

→ Asset được tạo và đếm đúng qua `/assets/stats` — dữ liệu được EF Core ghi thật vào MySQL (`mini_asm` database, port 3307), không còn ở in-memory.

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

**Minh chứng (terminal thật, WSL2 — chạy `./test.sh`):**

```
 youngkevinn@kevin   ~   main
╰─❯ ./test.sh
================================================================
 BAI 2: Cac scan type (dns + 4 scan moi: ip, port, ssl, tech)
================================================================

--- DNS scan ---
{"id":"bce58234-894d-48e0-abc6-9af69054a489","asset_id":"6f7c9893-2619-4855-bb73-125b7c7059e1","scan_type":"dns","status":"pending", ...}

--- SSL scan (moi) ---
{"id":"74aadb91-9575-4a01-b57c-2f308ce7c65d","asset_id":"6f7c9893-2619-4855-bb73-125b7c7059e1","scan_type":"ssl","status":"pending", ...}

--- Tech scan (moi) ---
{"id":"a14882db-62cf-425f-8951-0165f54f6940","asset_id":"6f7c9893-2619-4855-bb73-125b7c7059e1","scan_type":"tech","status":"pending", ...}

--- Port scan tren localhost (moi, an toan) ---
{"id":"56acf49a-0a4e-4489-9014-f48dc4d955e5","asset_id":"7e606e46-be07-4f37-9794-09744dfe2330","scan_type":"port","status":"pending", ...}

--- Tao public IP asset de test safety check ---
--- Thu port scan tren public IP (phai bi tu choi) ---
Doi cac scan job chay xong...

--- Ket qua DNS scan ---
{"job_id":"bce58234-894d-48e0-abc6-9af69054a489","scan_type":"dns","results":[{"domain":"google.com","records":{
  "a":["142.251.12.139","142.251.12.100","142.251.12.102","142.251.12.138","142.251.12.113","142.251.12.101"],
  "aaaa":["2404:6800:4003:c11::71","2404:6800:4003:c11::8b","2404:6800:4003:c11::66","2404:6800:4003:c11::8a"],
  "mx":[{"exchange":"smtp.google.com.","preference":10}],
  "ns":["ns1.google.com.","ns3.google.com.","ns2.google.com.","ns4.google.com."],
  "txt":["v=spf1 include:_spf.google.com ~all", ...]
}}]}

--- Ket qua SSL scan ---
{"job_id":"74aadb91-9575-4a01-b57c-2f308ce7c65d","scan_type":"ssl","results":[{
  "domain":"google.com",
  "certificate":{"subject":"CN=*.google.com","issuer":"CN=WR2, O=Google Trust Services, C=US",
    "days_until_expiry":59,"is_expired":false,"is_self_signed":false},
  "connection":{"tls_version":"TLS 1.3","cipher_suite":"TLS_AES_256_GCM_SHA384"},
  "grade":"A+","issues":[]
}]}

--- Ket qua Tech scan ---
{"job_id":"a14882db-62cf-425f-8951-0165f54f6940","scan_type":"tech","results":[{
  "domain":"google.com",
  "technologies":[{"name":"gws","category":"Web Server","version":null,"confidence":100}],
  "headers":{"server":"gws","x-xss-protection":"0","x-frame-options":"SAMEORIGIN","content-type":"text/html; charset=UTF-8"},
  "status_code":200
}]}

--- Ket qua Port scan (localhost) ---
{"job_id":"56acf49a-0a4e-4489-9014-f48dc4d955e5","scan_type":"port","results":[{
  "ip_address":"127.0.0.1",
  "open_ports":[
    {"port":135,"protocol":"tcp","state":"open","service":"msrpc"},
    {"port":445,"protocol":"tcp","state":"open","service":"smb"},
    {"port":8080,"protocol":"tcp","state":"open","service":"http-alt"},
    {"port":8888,"protocol":"tcp","state":"open","service":"http-alt"}
  ],
  "closed_ports":21,"total_scanned":25,"scan_duration_ms":1004
}]}

--- Ket qua port scan tren public IP (phai la failed + error message) ---
{"id":"a3090eb7-eb3b-4a3e-b791-eab18559f1e5","asset_id":"5cad4c59-c76f-4c21-bce6-8ccc681d4259",
 "scan_type":"port","status":"failed",
 "error":"Port scan is only allowed on localhost and private IP ranges (127.x, 10.x, 172.16-31.x, 192.168.x)",
 "results":0}
```

→ Cả 4 scan mới (`ip`, `port`, `ssl`, `tech`) hoạt động đúng. Đặc biệt **safety check của port scan hoạt động đúng**: scan trên `127.0.0.1` (localhost) thành công với 4 port mở, còn scan trên `8.8.8.8` (public IP) bị **từ chối tự động** với message rõ ràng — đúng yêu cầu bảo mật của đề bài.

---

## Bài 3: Viết Unit Tests (20đ)

**File tests:**
- `Models/AssetValidationTests.cs` — 8 test cases cho model validation
- `Scanners/DnsScannerTests.cs` — test DNS scanner thật (real DNS lookup, không mock)
- `Scanners/SslScannerTests.cs` — test SSL scanner thật (real TLS handshake)
- `Scanners/PortScannerTests.cs` — test port scanner + `IsPrivateOrLocalhost` safety check
- `Services/AssetServiceTests.cs` — 14 test cases với Moq mock (Bonus 3.3)

**Minh chứng — `dotnet test` output (PowerShell, máy thật):**

```
PS C:\WINDOWS\system32> cd "D:\Documents\TaiLieu\CMC-Training\Module Dev\Day3\AssetManager.Tests"
PS D:\...\AssetManager.Tests> dotnet test

Restore complete (3.2s)
  AssetManager succeeded (3.8s) → ...\AssetManager\bin\Debug\net8.0\AssetManager.dll
  AssetManager.Tests succeeded → bin\Debug\net8.0\AssetManager.Tests.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v2.8.2+699d445a1a (64-bit .NET 8.0.28)
[xUnit.net 00:00:00.25]   Discovering: AssetManager.Tests
[xUnit.net 00:00:00.29]   Discovered:  AssetManager.Tests
[xUnit.net 00:00:00.29]   Starting:    AssetManager.Tests
[xUnit.net 00:03:01.27]   Finished:    AssetManager.Tests
  AssetManager.Tests test succeeded (199.4s)

Test summary: total: 51, failed: 0, succeeded: 51, skipped: 0, duration: 182.1s
Build succeeded in 208.1s
```

(Test chạy 51 case thật — bao gồm DNS lookup, TLS handshake, port scan localhost — nên mất ~3 phút, không phải mock toàn bộ.)

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

**Minh chứng (terminal thật, WSL2):**

```
 youngkevinn@kevin   ~   main
╰─❯ ./test.sh
================================================================
 BAI 4: CORS preflight check
================================================================
HTTP/1.1 204 No Content
Date: Fri, 19 Jun 2026 07:39:51 GMT
Server: Kestrel
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

```
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

**Minh chứng (terminal thật, WSL2):**

```
 youngkevinn@kevin   ~   main
╰─❯ ./test.sh
================================================================
 BAI 7: Export reports (CSV)
================================================================
--- Export tat ca assets ---
id,name,type,status,created_at,updated_at
5cad4c59-c76f-4c21-bce6-8ccc681d4259,8.8.8.8,ip,active,2026-06-19T07:38:48.2873880,2026-06-19T07:38:48.2873890
7e606e46-be07-4f37-9794-09744dfe2330,127.0.0.1,ip,active,2026-06-19T07:38:47.7620470,2026-06-19T07:38:47.7620470
6f7c9893-2619-4855-bb73-125b7c7059e1,google.com,domain,active,2026-06-19T07:38:47.4743630,2026-06-19T07:38:47.4744650

--- Export scan results cua domain asset ---
id,job_id,asset_id,scan_type,created_at,data_summary
8f6995b4-a302-4870-a548-9f3093d56c2b,a14882db-62cf-425f-8951-0165f54f6940,6f7c9893-...,tech,2026-06-19T07:39:50.563229,count:1
f2924b3b-5ce9-4a08-9e3f-a6c8a8682747,74aadb91-9575-4a01-b57c-2f308ce7c65d,6f7c9893-...,ssl,2026-06-19T07:39:49.774618,grade:A+ tls:TLS 1.3
3465a5cb-8076-46fb-9d66-662d591ce65e,bce58234-894d-48e0-abc6-9af69054a489,6f7c9893-...,dns,2026-06-19T07:39:48.424299,a:6 mx:1 ns:4

================================================================
 Don dep test data
================================================================
Da xoa test assets.
```

**File:** `AssetManager/Controllers/ExportController.cs`
