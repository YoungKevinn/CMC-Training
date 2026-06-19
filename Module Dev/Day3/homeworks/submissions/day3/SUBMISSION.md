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

## Ghi chú triển khai

Project viết bằng C# thay vì Go. Xem [README.md](../../../README.md) để biết cách cài và chạy.

**Cách chạy nhanh:**
```bash
cd AssetManager
dotnet run
# Server tại http://localhost:8080
# Mở frontend/index.html trong browser
```

---

## Bài 1: Migrate sang Database

**Database chọn:** SQLite (không cần cài đặt thêm)

**Cách implement:**
- `AppDbContext` (EF Core) với 3 bảng: `Assets`, `ScanJobs`, `ScanResults`
- `EfAssetStorage` implements `IAssetStorage` — thay thế `MemoryStorage` từ Day 1
- Schema tự tạo lúc start server (`db.Database.EnsureCreated()`)
- File DB: `mini_asm.db` trong thư mục project

**Verify data persistence:**
1. Chạy server, tạo asset
2. Dừng server (Ctrl+C)
3. Chạy lại — asset vẫn còn

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

**Workflow:**
```
POST /assets/{id}/scan → 202 Accepted (job_id)
GET /scan-jobs/{job_id} → poll status
GET /scan-jobs/{job_id}/results → lấy kết quả
```

---

## Bài 3: Viết Unit Tests

**File tests:**
- `Models/AssetValidationTests.cs` — 8 test cases cho model validation
- `Scanners/DnsScannerTests.cs` — test DNS scanner thật
- `Scanners/SslScannerTests.cs` — test SSL scanner thật
- `Scanners/PortScannerTests.cs` — test port scanner + safety check
- `Services/AssetServiceTests.cs` — 14 test cases với Moq mock (Bonus)

**Chạy tests:**
```bash
cd AssetManager.Tests
dotnet test -v
dotnet test --collect:"XPlat Code Coverage"
```

---

## Bài 4: Tích hợp Frontend

**File:** `frontend/index.html` (single-file, không cần npm)

**Features:**
- Dashboard stats (total, domain, ip, service count)
- Danh sách assets với badge type/status
- Tạo asset mới
- Xóa asset
- Search realtime
- Trigger scan với dropdown chọn scan type
- Auto-poll status scan job mỗi 2 giây
- Hiển thị kết quả JSON

**CORS:** Đã cấu hình `AllowAll` trong `Program.cs`
