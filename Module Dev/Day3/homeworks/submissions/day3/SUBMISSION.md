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
- [ ] Bài 5: CI/CD với GitHub Actions (Bonus)
- [ ] Bài 6: Deploy với Docker Compose (Bonus)
- [ ] Bài 7: Tính năng EASM mới (Bonus)
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
