$BASE = "http://localhost:8080"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Asset Manager API - Full Test Suite" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# ----- Bai 5: Health Check -----
Write-Host "`n===== Bai 5: Health Check =====" -ForegroundColor Yellow
curl.exe -s "$BASE/health"

# ----- Bai 1.1: Stats (empty) -----
Write-Host "`n`n===== Bai 1.1: Stats (empty store) =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/stats"

# ----- Setup: Create test data -----
Write-Host "`n`n===== Setup: Create asset 1 =====" -ForegroundColor Yellow
curl.exe -s -X POST "$BASE/assets" -H "Content-Type: application/json" -d '{\"name\":\"example.com\",\"type\":\"domain\"}'

Write-Host "`n`n===== Setup: Create asset 2 =====" -ForegroundColor Yellow
curl.exe -s -X POST "$BASE/assets" -H "Content-Type: application/json" -d '{\"name\":\"192.168.1.1\",\"type\":\"ip\"}'

Write-Host "`n`n===== Setup: Create asset 3 (inactive) =====" -ForegroundColor Yellow
curl.exe -s -X POST "$BASE/assets" -H "Content-Type: application/json" -d '{\"name\":\"api.example.com\",\"type\":\"service\",\"status\":\"inactive\"}'

# ----- Bai 1.1: Stats (with data) -----
Write-Host "`n`n===== Bai 1.1: Stats (with data) =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/stats"

# ----- Bai 1.2: Count -----
Write-Host "`n`n===== Bai 1.2: Count all =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/count"

Write-Host "`n`n===== Bai 1.2: Count by type=domain =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/count?type=domain"

Write-Host "`n`n===== Bai 1.2: Count type=domain, status=active =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/count?type=domain&status=active"

# ----- Bai 2: Batch Create -----
Write-Host "`n`n===== Bai 2: Batch Create (success) =====" -ForegroundColor Yellow
curl.exe -s -X POST "$BASE/assets/batch" -H "Content-Type: application/json" -d '{\"assets\":[{\"name\":\"batch1.com\",\"type\":\"domain\"},{\"name\":\"batch2.com\",\"type\":\"domain\"},{\"name\":\"10.0.0.1\",\"type\":\"ip\"}]}'

Write-Host "`n`n===== Bai 2: Batch Create (fail - invalid type) =====" -ForegroundColor Yellow
curl.exe -s -w "`nHTTP Status: %{http_code}" -X POST "$BASE/assets/batch" -H "Content-Type: application/json" -d '{\"assets\":[{\"name\":\"good.com\",\"type\":\"domain\"},{\"name\":\"bad.com\",\"type\":\"invalid_type\"}]}'

# ----- Bai 3: Batch Delete -----
Write-Host "`n`n===== Bai 3: Setup - create 2 assets to delete =====" -ForegroundColor Yellow
$r1 = curl.exe -s -X POST "$BASE/assets" -H "Content-Type: application/json" -d '{\"name\":\"delete-me-1.com\",\"type\":\"domain\"}' | ConvertFrom-Json
$r2 = curl.exe -s -X POST "$BASE/assets" -H "Content-Type: application/json" -d '{\"name\":\"delete-me-2.com\",\"type\":\"domain\"}' | ConvertFrom-Json
$ID1 = $r1.id
$ID2 = $r2.id
Write-Host "Created: $ID1, $ID2"

Write-Host "`n===== Bai 3: Batch Delete (2 real + 1 fake) =====" -ForegroundColor Yellow
curl.exe -s -X DELETE "$BASE/assets/batch?ids=$ID1,$ID2,fake-uuid-123"

Write-Host "`n`n===== Bai 3: Verify deletion (should 404) =====" -ForegroundColor Yellow
curl.exe -s -w "`nHTTP Status: %{http_code}" "$BASE/assets/$ID1"

# ----- Bai 4: Concurrent Create -----
Write-Host "`n`n===== Bai 4: Concurrent Create (20 parallel) =====" -ForegroundColor Yellow
$jobs = 1..20 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($i, $base)
        curl.exe -s -X POST "$base/assets" -H "Content-Type: application/json" -d "{`\`"name`\`":`\`"concurrent-$i.com`\`",`\`"type`\`":`\`"domain`\`"}"
    } -ArgumentList $_, $BASE
}
$jobs | Wait-Job | Out-Null
$jobs | Remove-Job
Write-Host "All 20 requests completed."

Write-Host "`n===== Bai 4: Verify count =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/count?type=domain"

# ----- Bai 5: Health Check (after data) -----
Write-Host "`n`n===== Bai 5: Health Check (after data) =====" -ForegroundColor Yellow
curl.exe -s "$BASE/health"

# ----- Bai 6: Pagination -----
Write-Host "`n`n===== Bai 6: List page 1, limit 5 =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets?page=1&limit=5"

Write-Host "`n`n===== Bai 6: Page 2, limit 5, type=domain =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets?page=2&limit=5&type=domain"

# ----- Bai 7: Search -----
Write-Host "`n`n===== Bai 7: Search 'example' =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/search?q=example"

Write-Host "`n`n===== Bai 7: Search '.com' =====" -ForegroundColor Yellow
curl.exe -s "$BASE/assets/search?q=.com"

Write-Host "`n`n================================================" -ForegroundColor Cyan
Write-Host "  DONE - Copy output vao SUBMISSION.md" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
