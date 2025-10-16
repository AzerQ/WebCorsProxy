#!/bin/bash

# Конфигурация
SERVER_URL="${SERVER_URL:-http://localhost:8000}"
API_TOKEN="${API_TOKEN:-your-api-key-1}"

# Цвета
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}????????????????????????????????????????${NC}"
echo -e "${GREEN}?? Testing WebCorsProxy API${NC}"
echo -e "${BLUE}????????????????????????????????????????${NC}"
echo -e "Server: ${YELLOW}$SERVER_URL${NC}\n"

# Тест 1: Root endpoint
echo -e "${YELLOW}Test 1: Root endpoint (/)${NC}"
curl -s -o /dev/null -w "HTTP Status: %{http_code}\n" "$SERVER_URL/"
echo ""

# Тест 2: Simple CORS proxy
echo -e "${YELLOW}Test 2: Simple CORS proxy (/proxy)${NC}"
echo "Request: GET /proxy?url=https://httpbin.org/get"
RESPONSE=$(curl -s "$SERVER_URL/proxy?url=https://httpbin.org/get")
echo "Response preview:"
echo "$RESPONSE" | head -n 10
echo ""

# Тест 3: Proxy with JSON response
echo -e "${YELLOW}Test 3: Proxy JSON API (/proxy)${NC}"
echo "Request: GET /proxy?url=https://httpbin.org/json"
curl -s "$SERVER_URL/proxy?url=https://httpbin.org/json" | head -n 5
echo -e "\n"

# Тест 4: Web endpoint without token (should fail or require auth)
echo -e "${YELLOW}Test 4: Web endpoint without token (/web)${NC}"
echo "Request: GET /web?url=https://example.com"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/web?url=https://example.com")
echo "HTTP Status: $HTTP_CODE"
if [ "$HTTP_CODE" = "401" ]; then
    echo -e "${GREEN}? Auth protection works${NC}"
elif [ "$HTTP_CODE" = "200" ]; then
    echo -e "${YELLOW}??  Endpoint works without token${NC}"
fi
echo ""

# Тест 5: Web endpoint with token
echo -e "${YELLOW}Test 5: Web endpoint with token (/web)${NC}"
echo "Request: GET /web?url=https://example.com&token=$API_TOKEN"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/web?url=https://example.com&token=$API_TOKEN")
echo "HTTP Status: $HTTP_CODE"
if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}? Authorized request successful${NC}"
else
    echo -e "${YELLOW}??  Unexpected status code${NC}"
fi
echo ""

# Тест 6: Proxy with HTML content
echo -e "${YELLOW}Test 6: Proxy HTML page (/proxy)${NC}"
echo "Request: GET /proxy?url=https://example.com"
RESPONSE=$(curl -s "$SERVER_URL/proxy?url=https://example.com")
if echo "$RESPONSE" | grep -q "Example Domain"; then
    echo -e "${GREEN}? HTML content received${NC}"
    echo "Content preview:"
    echo "$RESPONSE" | grep -i "title" | head -n 1
else
    echo -e "${YELLOW}??  Unexpected response${NC}"
fi
echo ""

# Тест 7: Invalid URL handling
echo -e "${YELLOW}Test 7: Invalid URL handling (/proxy)${NC}"
echo "Request: GET /proxy?url=invalid-url"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/proxy?url=invalid-url")
echo "HTTP Status: $HTTP_CODE"
if [ "$HTTP_CODE" = "400" ] || [ "$HTTP_CODE" = "500" ]; then
    echo -e "${GREEN}? Error handling works${NC}"
else
    echo -e "${YELLOW}??  Unexpected status: $HTTP_CODE${NC}"
fi
echo ""

# Тест 8: Missing URL parameter
echo -e "${YELLOW}Test 8: Missing URL parameter (/proxy)${NC}"
echo "Request: GET /proxy"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL/proxy")
echo "HTTP Status: $HTTP_CODE"
if [ "$HTTP_CODE" = "400" ]; then
    echo -e "${GREEN}? Validation works${NC}"
else
    echo -e "${YELLOW}??  Unexpected status: $HTTP_CODE${NC}"
fi
echo ""

# Тест 9: CORS headers check
echo -e "${YELLOW}Test 9: CORS headers check (/proxy)${NC}"
echo "Request: GET /proxy?url=https://httpbin.org/get"
HEADERS=$(curl -s -I "$SERVER_URL/proxy?url=https://httpbin.org/get")
if echo "$HEADERS" | grep -qi "access-control-allow-origin"; then
    echo -e "${GREEN}? CORS headers present${NC}"
    echo "$HEADERS" | grep -i "access-control"
else
    echo -e "${YELLOW}??  CORS headers not found${NC}"
fi
echo ""

# Итоги
echo -e "${BLUE}????????????????????????????????????????${NC}"
echo -e "${GREEN}? Testing completed!${NC}"
echo -e "${BLUE}????????????????????????????????????????${NC}"
echo ""
echo -e "${YELLOW}Additional manual tests:${NC}"
echo "1. Browser test: Open $SERVER_URL in browser"
echo "2. Proxy test: curl '$SERVER_URL/proxy?url=https://api.github.com'"
echo "3. Web test: curl '$SERVER_URL/web?url=https://example.com&token=$API_TOKEN'"
echo ""
echo -e "${YELLOW}Environment variables:${NC}"
echo "- SERVER_URL: Set server URL (current: $SERVER_URL)"
echo "- API_TOKEN: Set API token (current: ${API_TOKEN:0:10}...)"
echo ""
echo "Example: SERVER_URL=https://your-domain.com API_TOKEN=your-token ./test-api.sh"
