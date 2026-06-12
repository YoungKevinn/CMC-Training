# Homework Submission

**Họ tên:** Đỗ Minh Khoa

## Ngôn ngữ: C# / ASP.NET Core 8

> Project không dùng Go mà sử dụng C# với ASP.NET Core. Xem [README.md] để biết cách cài đặt và chạy.

## Các bài đã hoàn thành

- [x] Bài 1: Statistics APIs
- [x] Bài 2: Batch Create
- [x] Bài 3: Batch Delete
- [x] Bài 4: Concurrent-safe Create
- [x] Bài 5: In-memory Health Check
- [x] Bài 6: Pagination (Bonus)
- [x] Bài 7: Search (Bonus)

## Test Evidence

### Bài 1: Statistics APIs

```
# Stats (empty store)
GET /assets/stats
{"total":0,"by_type":{},"by_status":{}}

# Tạo 3 assets: 1 domain, 1 ip, 1 service (inactive)

# Stats (with data)
GET /assets/stats
{"total":3,"by_type":{"domain":1,"ip":1,"service":1},"by_status":{"active":2,"inactive":1}}

# Count all
GET /assets/count
{"count":3,"filters":{"type":null,"status":null}}

# Count by type=domain
GET /assets/count?type=domain
{"count":1,"filters":{"type":"domain","status":null}}

# Count by type=domain&status=active
GET /assets/count?type=domain&status=active
{"count":1,"filters":{"type":"domain","status":"active"}}
```

### Bài 2: Batch Create

```
# Success case - tạo 3 assets cùng lúc
POST /assets/batch
{"created":3,"ids":["513796fe-fae8-4b36-bb34-d763829b3b02","cb15b312-0f5e-4af4-a907-03d51f8f1743","9056e3a8-4ea2-48e4-800c-2d087f90d954"]}

# Error case - invalid type -> 400, none created (all-or-nothing)
POST /assets/batch
{"error":"asset[1]: invalid type 'invalid_type', must be one of: domain, ip, service"}
HTTP Status: 400
```

### Bài 3: Batch Delete

```
# Tạo 2 assets để xóa
Created: 9e35472b-a2e1-43c2-82a8-593a4e15082b, 42b80379-d9e9-460c-8a6b-08ad238db6d0

# Batch delete 2 real + 1 fake ID
DELETE /assets/batch?ids=9e35472b-...,42b80379-...,fake-uuid-123
{"deleted":2,"not_found":1}

# Verify deletion -> 404
GET /assets/9e35472b-a2e1-43c2-82a8-593a4e15082b
{"error":"asset not found"}
HTTP Status: 404
```

### Bài 4: Concurrent-safe Create

```
# Bắn 20 request POST /assets song song
All 20 requests completed.

# Verify count -> 23 domain (1 ban đầu + 2 batch + 20 concurrent)
GET /assets/count?type=domain
{"count":23,"filters":{"type":"domain","status":null}}

# Không crash, không duplicate, không mất data
```

### Bài 5: Health Check

```
# Health check lúc mới start (empty)
GET /health
{"status":"ok","storage":{"type":"in-memory","asset_count":0},"uptime_seconds":239,"timestamp":"2026-06-12T08:32:49Z"}

# Health check sau khi tạo assets
GET /health
{"status":"ok","storage":{"type":"in-memory","asset_count":26},"uptime_seconds":243,"timestamp":"2026-06-12T08:32:53Z"}
```

### Bài 6: Pagination (Bonus)

```
# Page 1, limit 5
GET /assets?page=1&limit=5
{"data":[...],"pagination":{"page":1,"limit":5,"total":26,"total_pages":6}}

# Page 2, limit 5, type=domain
GET /assets?page=2&limit=5&type=domain
{"data":[...],"pagination":{"page":2,"limit":5,"total":23,"total_pages":5}}
```

### Bài 7: Search (Bonus)

```
# Search "example" -> tìm đúng 2 assets có chứa "example"
GET /assets/search?q=example
[
  {"name":"example.com","type":"domain","status":"active",...},
  {"name":"api.example.com","type":"service","status":"inactive",...}
]

# Search ".com" -> tìm tất cả assets có chứa ".com" (24 kết quả)
GET /assets/search?q=.com
[{"name":"example.com",...},{"name":"api.example.com",...},{"name":"batch1.com",...},...]
```