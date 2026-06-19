# Asset Manager — Day 3

EASM (External Attack Surface Management) API viết bằng **C# / ASP.NET Core 8**.

Tiếp nối từ Day 1 (Asset CRUD với in-memory storage), Day 3 thêm:
- **Bài 1**: MySQL database thay thế in-memory storage (EF Core + Pomelo)
- **Bài 2**: Scan API với 9 loại scanner (dns, whois, subdomain, cert_trans, asn, ip, port, ssl, tech)
- **Bài 3**: Unit tests với xUnit + Moq
- **Bài 4**: Frontend dashboard + CORS
- **Bài 5 (Bonus)**: CI/CD với GitHub Actions (build, test, security scans)
- **Bài 6 (Bonus)**: Docker Compose (MySQL + API)
- **Bài 7 (Bonus)**: Export Reports — CSV/JSON

---

## Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server đang chạy (local hoặc Docker)

```bash
dotnet --version   # cần >= 8.0
```

**Database:** MySQL qua EF Core + Pomelo.EntityFrameworkCore.MySql.

Connection string trong `appsettings.json`:
```json
"DefaultConnection": "Server=localhost;Port=3307;Database=mini_asm;User=root;Password=root;"
```

Đổi `Port`/`User`/`Password` cho phù hợp với MySQL instance của bạn. Database `mini_asm` và toàn bộ schema (`Assets`, `ScanJobs`, `ScanResults`) tự tạo khi server start lần đầu (`db.Database.EnsureCreated()`) — không cần chạy migration tay.

---

## Cách chạy

### Backend

```bash
cd AssetManager
dotnet restore
dotnet run
```

Server chạy tại `http://localhost:8080`. Schema MySQL tự tạo lần đầu kết nối.

### Frontend

Chỉ cần mở file trong browser:

```
frontend/index.html
```

Hoặc dùng Live Server nếu có VS Code extension.

### Tests

```bash
cd AssetManager.Tests
dotnet test

# Với coverage report
dotnet test --collect:"XPlat Code Coverage"
```

---

## Cấu trúc project

```
Day3/
├── .github/workflows/ci.yml       # CI: build, test, security scans (Bài 5)
├── AssetManager/                  # Web API chính
│   ├── Controllers/               # HTTP endpoints
│   │   ├── AssetsController.cs    # CRUD + scan triggers
│   │   ├── ScanJobsController.cs  # /scan-jobs endpoints
│   │   ├── ExportController.cs    # CSV/JSON export (NEW Day 3, Bài 7)
│   │   └── HealthController.cs
│   ├── Data/
│   │   └── AppDbContext.cs        # EF Core DbContext (MySQL via Pomelo)
│   ├── DTOs/                      # Request / Response objects
│   ├── Models/                    # Asset, ScanJob, ScanResult
│   ├── Scanners/                  # 9 scanner implementations
│   │   ├── DnsScanner.cs
│   │   ├── WhoisScanner.cs
│   │   ├── SubdomainScanner.cs    # crt.sh
│   │   ├── CertTransScanner.cs   # crt.sh
│   │   ├── AsnScanner.cs         # ip-api.com
│   │   ├── IpScanner.cs          # ip-api.com (NEW Day 3)
│   │   ├── PortScanner.cs        # TCP scan localhost only (NEW Day 3)
│   │   ├── SslScanner.cs         # TLS cert inspection (NEW Day 3)
│   │   └── TechScanner.cs        # HTTP header detection (NEW Day 3)
│   ├── Services/                  # Business logic
│   ├── Storage/                   # EF Core repository implementations
│   ├── Workers/
│   │   └── ScanWorker.cs         # Background service for async scans
│   └── Program.cs
├── AssetManager.Tests/            # xUnit tests
│   ├── Models/AssetValidationTests.cs
│   ├── Scanners/DnsScannerTests.cs
│   ├── Scanners/SslScannerTests.cs
│   ├── Scanners/PortScannerTests.cs
│   └── Services/AssetServiceTests.cs  # Moq mock tests
├── frontend/
│   └── index.html                 # Single-file vanilla JS dashboard
├── Dockerfile                      # Multi-stage build (Bài 6)
├── docker-compose.yml              # db (MySQL) + backend (Bài 6)
├── api.yml                         # OpenAPI 3.0 spec
└── homeworks/submissions/day3/SUBMISSION.md
```

---

## API Endpoints

### Assets (Day 1 + scan triggers)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST   | `/assets` | Tạo asset |
| GET    | `/assets/{id}` | Lấy theo ID |
| PUT    | `/assets/{id}` | Cập nhật |
| DELETE | `/assets/{id}` | Xóa |
| GET    | `/assets` | Danh sách có phân trang |
| GET    | `/assets/stats` | Thống kê |
| GET    | `/assets/count` | Đếm theo filter |
| GET    | `/assets/search?q=` | Tìm theo tên |
| POST   | `/assets/batch` | Tạo nhiều |
| DELETE | `/assets/batch?ids=` | Xóa nhiều |
| POST   | `/assets/{id}/scan` | Khởi chạy scan |
| GET    | `/assets/{id}/scans` | Lịch sử scan |
| GET    | `/assets/{id}/results` | Tất cả kết quả |
| GET    | `/assets/{id}/dns` | Kết quả DNS |
| GET    | `/assets/{id}/whois` | Kết quả WHOIS |
| GET    | `/assets/{id}/subdomains` | Kết quả subdomain |

### Scan Jobs

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET    | `/scan-jobs/{id}` | Trạng thái job |
| GET    | `/scan-jobs/{id}/results` | Kết quả job |

### Health

| Method | Endpoint |
|--------|----------|
| GET    | `/health` |

### Export (Bài 7 — Bonus)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET    | `/export/assets.csv` | Tất cả assets dạng CSV |
| GET    | `/export/assets.json` | Tất cả assets dạng JSON |
| GET    | `/assets/{id}/export/results.json` | Scan results của asset (JSON download) |
| GET    | `/assets/{id}/export/results.csv` | Scan results của asset (CSV download) |
| GET    | `/scan-jobs/{id}/export/results.json` | Results của 1 job (JSON download) |

---

## Scan Types

| Type | Loại | Asset | Nguồn dữ liệu |
|------|------|-------|----------------|
| `dns` | Passive | domain | System DNS |
| `whois` | Passive | domain | WHOIS TCP port 43 |
| `subdomain` | Passive | domain | crt.sh |
| `cert_trans` | Passive | domain | crt.sh |
| `asn` | Passive | ip | ip-api.com |
| `all` | Passive | domain | Chạy: dns + whois + subdomain + cert_trans |
| `ip` | Passive | ip | ip-api.com + reverse DNS |
| `port` | Active ⚠️ | ip | TCP scan (localhost/private only) |
| `ssl` | Active | domain/service | TLS handshake |
| `tech` | Active | domain/service | HTTP headers + body |

> ⚠️ Port scan chỉ hoạt động với localhost và private IP (10.x, 192.168.x, 172.16-31.x)

---

## Ví dụ test nhanh

```bash
# Tạo domain asset
curl -X POST http://localhost:8080/assets \
  -H "Content-Type: application/json" \
  -d '{"name":"google.com","type":"domain"}'

# Tạo IP asset
curl -X POST http://localhost:8080/assets \
  -H "Content-Type: application/json" \
  -d '{"name":"127.0.0.1","type":"ip"}'

# Start DNS scan (thay {id} bằng ID trả về ở trên)
curl -X POST http://localhost:8080/assets/{id}/scan \
  -H "Content-Type: application/json" \
  -d '{"scan_type":"dns"}'

# Kiểm tra status
curl http://localhost:8080/scan-jobs/{job_id}

# Lấy kết quả
curl http://localhost:8080/scan-jobs/{job_id}/results

# Port scan localhost
curl -X POST http://localhost:8080/assets/{ip_asset_id}/scan \
  -H "Content-Type: application/json" \
  -d '{"scan_type":"port"}'

# Health check
curl http://localhost:8080/health
```

---

## Ghi chú kỹ thuật

- **Database**: MySQL với EF Core (Pomelo.EntityFrameworkCore.MySql) — schema tự tạo qua `EnsureCreated()`, các cột có index (`Type`, `Status`, `ScanType`, ...) được giới hạn `HasMaxLength` vì MySQL không index được cột `longtext`
- **Scan async**: Mỗi scan chạy background qua `Channel<string>` + `IHostedService` (`ScanWorker`)
- **CORS**: Đã cấu hình `AllowAll` cho frontend gọi được từ bất kỳ origin
- **JSON**: Snake_case tự động cho toàn bộ API
- **Port scanner safety**: Từ chối public IP, chỉ cho phép `127.x`, `10.x`, `192.168.x`, `172.16-31.x`
