# Asset Manager API

API quản lý assets (domain, IP, service) dùng in-memory storage.

Viết bằng C# với ASP.NET Core 8 (không dùng Go).

## Cách chạy

Cần cài .NET 8 SDK trước: https://dotnet.microsoft.com/download/dotnet/8.0

```bash
dotnet --version   # check đã cài chưa, cần >= 8.0
```

```bash
cd AssetManager
dotnet run
```

Server chạy ở http://localhost:8080

## Cấu trúc project

Mình chia theo kiểu Clean Architecture: Controller → Service → Storage → Model

```
AssetManager/
├── Controllers/       # Xử lý HTTP request
├── Services/          # Business logic, validate
├── Storage/           # Lưu trữ data (in-memory)
├── Models/            # Định nghĩa Asset
├── DTOs/              # Request/Response objects
└── Program.cs         # Khởi tạo app, cấu hình DI
```

## Endpoints

- `POST /assets` — tạo 1 asset
- `GET /assets/{id}` — lấy theo ID
- `PUT /assets/{id}` — update
- `DELETE /assets/{id}` — xóa
- `GET /assets/stats` — thống kê theo type/status
- `GET /assets/count?type=&status=` — đếm theo filter
- `POST /assets/batch` — tạo nhiều cùng lúc
- `DELETE /assets/batch?ids=id1,id2` — xóa nhiều
- `GET /health` — health check
- `GET /assets?page=1&limit=20&type=&status=` — list có phân trang
- `GET /assets/search?q=keyword` — tìm theo tên

## Concurrent safety

Storage dùng `ReaderWriterLockSlim` (tương đương `sync.RWMutex` bên Go) để đảm bảo thread-safe khi nhiều request ghi cùng lúc.

## Test nhanh

```bash
# tạo asset
curl -X POST http://localhost:8080/assets \
  -H "Content-Type: application/json" \
  -d '{"name":"example.com","type":"domain"}'

# batch create
curl -X POST http://localhost:8080/assets/batch \
  -H "Content-Type: application/json" \
  -d '{"assets":[{"name":"a.com","type":"domain"},{"name":"1.1.1.1","type":"ip"}]}'

# xem stats
curl http://localhost:8080/assets/stats

# health check
curl http://localhost:8080/health
```

Hoặc chạy `bash test_all.sh` để test hết tất cả endpoints.