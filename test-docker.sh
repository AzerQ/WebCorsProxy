#!/bin/bash

# ����� ��� ������
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}?? Testing Docker build and deployment locally...${NC}\n"

# ��� ������ � ����������
IMAGE_NAME="webcorsproxy-test"
CONTAINER_NAME="proxyserver-test"
PORT=8001

# ������� ��� �������
cleanup() {
    echo -e "\n${YELLOW}?? Cleaning up...${NC}"
    docker stop $CONTAINER_NAME 2>/dev/null || true
    docker rm $CONTAINER_NAME 2>/dev/null || true
    docker rmi $IMAGE_NAME 2>/dev/null || true
    echo -e "${GREEN}? Cleanup completed${NC}"
}

# ��������� Ctrl+C
trap cleanup EXIT

# 1. ������ ������
echo -e "${YELLOW}?? Building Docker image...${NC}"
if docker build -t $IMAGE_NAME .; then
    echo -e "${GREEN}? Build successful${NC}\n"
else
    echo -e "${RED}? Build failed${NC}"
    exit 1
fi

# 2. ������ ����������
echo -e "${YELLOW}?? Starting container...${NC}"
if docker run -d \
    --name $CONTAINER_NAME \
    -p $PORT:8080 \
    -v "$(pwd)/appsettings.json:/app/appsettings.json:ro" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    $IMAGE_NAME; then
    echo -e "${GREEN}? Container started${NC}\n"
else
    echo -e "${RED}? Failed to start container${NC}"
    exit 1
fi

# 3. �������� �������
echo -e "${YELLOW}? Waiting for application to start...${NC}"
sleep 5

# 4. �������� �������
echo -e "${YELLOW}?? Container status:${NC}"
docker ps -f name=$CONTAINER_NAME

# 5. �������� �����
echo -e "\n${YELLOW}?? Container logs:${NC}"
docker logs $CONTAINER_NAME

# 6. ������������ ����������
echo -e "\n${YELLOW}?? Testing endpoints...${NC}"

# ���� 1: ������� ��������
echo -e "\n${YELLOW}Test 1: GET /${NC}"
if curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/" | grep -q "200\|301\|302"; then
    echo -e "${GREEN}? Root endpoint works${NC}"
else
    echo -e "${RED}? Root endpoint failed${NC}"
fi

# ���� 2: /proxy ��������
echo -e "\n${YELLOW}Test 2: GET /proxy?url=https://httpbin.org/get${NC}"
RESPONSE=$(curl -s -w "\n%{http_code}" "http://localhost:$PORT/proxy?url=https://httpbin.org/get")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}? Proxy endpoint works${NC}"
    echo "Response preview:"
    echo "$RESPONSE" | head -n-1 | head -n 5
else
    echo -e "${RED}? Proxy endpoint failed (HTTP $HTTP_CODE)${NC}"
fi

# ���� 3: /web �������� (��� ������, ������ ������� ������ ��� ��������)
echo -e "\n${YELLOW}Test 3: GET /web?url=https://example.com${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/web?url=https://example.com")
echo -e "HTTP Status: $HTTP_CODE"
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "401" ]; then
    echo -e "${GREEN}? Web endpoint responds${NC}"
else
    echo -e "${RED}? Web endpoint failed (HTTP $HTTP_CODE)${NC}"
fi

# 7. ���������� � ��������
echo -e "\n${YELLOW}?? Container resources:${NC}"
docker stats --no-stream $CONTAINER_NAME

# 8. �������� ����������
echo -e "\n${YELLOW}????????????????????????????????????????${NC}"
echo -e "${GREEN}? Local testing completed!${NC}"
echo -e "${YELLOW}????????????????????????????????????????${NC}"
echo -e "\n${YELLOW}?? You can:${NC}"
echo -e "  - View logs: ${GREEN}docker logs -f $CONTAINER_NAME${NC}"
echo -e "  - Access app: ${GREEN}http://localhost:$PORT${NC}"
echo -e "  - Test proxy: ${GREEN}curl 'http://localhost:$PORT/proxy?url=https://example.com'${NC}"
echo -e "  - Stop container: ${GREEN}docker stop $CONTAINER_NAME${NC}"
echo -e "\n${YELLOW}Press Ctrl+C to stop and cleanup${NC}"

# ������ ������ ����������
read -p "Press Enter to cleanup and exit..."
