#!/bin/bash
# Chạy script này từ thư mục Module Dev/Day3 sau khi đã start server (dotnet run trong AssetManager/)
# Output từng bước in ra terminal, copy nguyên đoạn này gửi cho thầy.

set -e
API="http://localhost:8080"

echo "================================================================"
echo " BAI 1: Health check + tao asset (proof MySQL hoat dong)"
echo "================================================================"
curl -s "$API/health"; echo

echo ""
echo "--- Tao domain asset ---"
DOMAIN_RESP=$(curl -s -X POST "$API/assets" -H "Content-Type: application/json" -d '{"name":"google.com","type":"domain"}')
echo "$DOMAIN_RESP"
DOMAIN_ID=$(echo "$DOMAIN_RESP" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- Tao IP asset ---"
IP_RESP=$(curl -s -X POST "$API/assets" -H "Content-Type: application/json" -d '{"name":"127.0.0.1","type":"ip"}')
echo "$IP_RESP"
IP_ID=$(echo "$IP_RESP" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- Stats sau khi tao 2 assets ---"
curl -s "$API/assets/stats"; echo

echo ""
echo "================================================================"
echo " BAI 2: Cac scan type (dns + 4 scan moi: ip, port, ssl, tech)"
echo "================================================================"

echo ""
echo "--- DNS scan ---"
DNS_JOB=$(curl -s -X POST "$API/assets/$DOMAIN_ID/scan" -H "Content-Type: application/json" -d '{"scan_type":"dns"}')
echo "$DNS_JOB"
DNS_JOB_ID=$(echo "$DNS_JOB" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- SSL scan (moi) ---"
SSL_JOB=$(curl -s -X POST "$API/assets/$DOMAIN_ID/scan" -H "Content-Type: application/json" -d '{"scan_type":"ssl"}')
echo "$SSL_JOB"
SSL_JOB_ID=$(echo "$SSL_JOB" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- Tech scan (moi) ---"
TECH_JOB=$(curl -s -X POST "$API/assets/$DOMAIN_ID/scan" -H "Content-Type: application/json" -d '{"scan_type":"tech"}')
echo "$TECH_JOB"
TECH_JOB_ID=$(echo "$TECH_JOB" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- Port scan tren localhost (moi, an toan) ---"
PORT_JOB=$(curl -s -X POST "$API/assets/$IP_ID/scan" -H "Content-Type: application/json" -d '{"scan_type":"port"}')
echo "$PORT_JOB"
PORT_JOB_ID=$(echo "$PORT_JOB" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo ""
echo "--- Tao public IP asset de test safety check ---"
PUBLIC_RESP=$(curl -s -X POST "$API/assets" -H "Content-Type: application/json" -d '{"name":"8.8.8.8","type":"ip"}')
PUBLIC_ID=$(echo "$PUBLIC_RESP" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo "--- Thu port scan tren public IP (phai bi tu choi) ---"
PUBLIC_PORT_JOB=$(curl -s -X POST "$API/assets/$PUBLIC_ID/scan" -H "Content-Type: application/json" -d '{"scan_type":"port"}')
PUBLIC_PORT_ID=$(echo "$PUBLIC_PORT_JOB" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

echo "Doi cac scan job chay xong..."
wait_for_job() {
  for i in $(seq 1 30); do
    status=$(curl -s "$API/scan-jobs/$1" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
    if [ "$status" = "completed" ] || [ "$status" = "failed" ] || [ "$status" = "partial" ]; then
      return
    fi
    sleep 1
  done
}
wait_for_job "$DNS_JOB_ID"
wait_for_job "$SSL_JOB_ID"
wait_for_job "$TECH_JOB_ID"
wait_for_job "$PORT_JOB_ID"
wait_for_job "$PUBLIC_PORT_ID"

echo ""
echo "--- Ket qua DNS scan ---"
curl -s "$API/scan-jobs/$DNS_JOB_ID/results"; echo

echo ""
echo "--- Ket qua SSL scan ---"
curl -s "$API/scan-jobs/$SSL_JOB_ID/results"; echo

echo ""
echo "--- Ket qua Tech scan ---"
curl -s "$API/scan-jobs/$TECH_JOB_ID/results"; echo

echo ""
echo "--- Ket qua Port scan (localhost) ---"
curl -s "$API/scan-jobs/$PORT_JOB_ID/results"; echo

echo ""
echo "--- Ket qua port scan tren public IP (phai la failed + error message) ---"
curl -s "$API/scan-jobs/$PUBLIC_PORT_ID"; echo

echo ""
echo "================================================================"
echo " BAI 3: Unit tests"
echo "================================================================"
echo "(Chay rieng: cd AssetManager.Tests && dotnet test)"

echo ""
echo "================================================================"
echo " BAI 4: CORS preflight check"
echo "================================================================"
curl -s -i -X OPTIONS "$API/assets" -H "Origin: http://example.com" -H "Access-Control-Request-Method: POST" | head -10

echo ""
echo "================================================================"
echo " BAI 7: Export reports (CSV)"
echo "================================================================"
echo "--- Export tat ca assets ---"
curl -s "$API/export/assets.csv"; echo

echo ""
echo "--- Export scan results cua domain asset ---"
curl -s "$API/assets/$DOMAIN_ID/export/results.csv"; echo

echo ""
echo "================================================================"
echo " Don dep test data"
echo "================================================================"
curl -s -X DELETE "$API/assets/$DOMAIN_ID" -o /dev/null
curl -s -X DELETE "$API/assets/$IP_ID" -o /dev/null
curl -s -X DELETE "$API/assets/$PUBLIC_ID" -o /dev/null
echo "Da xoa test assets."
