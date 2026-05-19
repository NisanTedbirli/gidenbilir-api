#!/bin/bash

# Giden Bilir Messaging System - Test Commands
# Copy-paste these commands to test all endpoints

BASE_URL="http://localhost:5000"

# ============================================
# SETUP: Register two users
# ============================================

echo "=== Step 1: Register User 1 ==="
REGISTER_1=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "password": "Password123",
    "nationalityId": 1
  }')

echo "$REGISTER_1" | grep -o '"token":"[^"]*' | head -1
USER1_TOKEN=$(echo "$REGISTER_1" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
USER1_ID=$(echo "$REGISTER_1" | grep -o '"userId":[0-9]*' | cut -d':' -f2)
echo "User 1 Token: $USER1_TOKEN"
echo "User 1 ID: $USER1_ID"
echo ""

echo "=== Step 2: Register User 2 ==="
REGISTER_2=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Alice Smith",
    "email": "alice@example.com",
    "password": "Password123",
    "nationalityId": 2
  }')

USER2_TOKEN=$(echo "$REGISTER_2" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
USER2_ID=$(echo "$REGISTER_2" | grep -o '"userId":[0-9]*' | cut -d':' -f2)
echo "User 2 Token: $USER2_TOKEN"
echo "User 2 ID: $USER2_ID"
echo ""

# ============================================
# TEST 1: Create Conversation (User1 → User2)
# ============================================

echo "=== Test 1: POST /api/conversations (Create conversation) ==="
CONV_CREATE=$(curl -s -X POST "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"participantId\": $USER2_ID}")

echo "$CONV_CREATE"
CONVERSATION_ID=$(echo "$CONV_CREATE" | grep -o '"id":[0-9]*' | cut -d':' -f2)
echo "Conversation ID: $CONVERSATION_ID"
echo ""

# ============================================
# TEST 2: Send Message (User1)
# ============================================

echo "=== Test 2: POST /api/conversations/{id}/messages (Send message) ==="
curl -s -X POST "$BASE_URL/api/conversations/$CONVERSATION_ID/messages" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "Merhaba! Nasılsın?"}' | jq .
echo ""

# ============================================
# TEST 3: Send Reply Message (User2)
# ============================================

echo "=== Test 3: Send reply message from User2 ==="
MESSAGE_REPLY=$(curl -s -X POST "$BASE_URL/api/conversations/$CONVERSATION_ID/messages" \
  -H "Authorization: Bearer $USER2_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "İyiyim, sen nasılsın?"}')

echo "$MESSAGE_REPLY" | jq .
MESSAGE_ID=$(echo "$MESSAGE_REPLY" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
echo "Message ID: $MESSAGE_ID"
echo ""

# ============================================
# TEST 4: Get Conversations (User1)
# ============================================

echo "=== Test 4: GET /api/conversations (List conversations) ==="
curl -s -X GET "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" | jq .
echo ""

# ============================================
# TEST 5: Get Messages (Paginated, Page 1)
# ============================================

echo "=== Test 5: GET /api/conversations/{id}/messages (Page 1) ==="
curl -s -X GET "$BASE_URL/api/conversations/$CONVERSATION_ID/messages?page=1&pageSize=10" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" | jq .
echo ""

# ============================================
# TEST 6: Send More Messages for Pagination Test
# ============================================

echo "=== Test 6: Send 5 more messages for pagination test ==="
for i in {1..5}; do
  curl -s -X POST "$BASE_URL/api/conversations/$CONVERSATION_ID/messages" \
    -H "Authorization: Bearer $USER1_TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"content\": \"Test message $i\"}" > /dev/null
  echo "Message $i sent"
done
echo ""

# ============================================
# TEST 7: Get Messages (Paginated, Custom Size)
# ============================================

echo "=== Test 7: GET /api/conversations/{id}/messages (pageSize=3) ==="
curl -s -X GET "$BASE_URL/api/conversations/$CONVERSATION_ID/messages?page=1&pageSize=3" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" | jq .
echo ""

# ============================================
# TEST 8: Get Messages (Page 2)
# ============================================

echo "=== Test 8: GET /api/conversations/{id}/messages (Page 2) ==="
curl -s -X GET "$BASE_URL/api/conversations/$CONVERSATION_ID/messages?page=2&pageSize=3" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" | jq .
echo ""

# ============================================
# TEST 9: Delete Message (Sender only)
# ============================================

echo "=== Test 9: DELETE /api/conversations/{id}/messages/{messageId} ==="
echo "Deleting message ID: $MESSAGE_ID"
curl -s -X DELETE "$BASE_URL/api/conversations/$CONVERSATION_ID/messages/$MESSAGE_ID" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" -w "\nStatus: %{http_code}\n"
echo ""

# ============================================
# TEST 10: Error Case - Try Delete as Non-Sender
# ============================================

echo "=== Test 10: Error - Delete as non-sender ==="
# Send a message from User1
FIRST_MSG=$(curl -s -X POST "$BASE_URL/api/conversations/$CONVERSATION_ID/messages" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "This message is from User1"}')

FIRST_MSG_ID=$(echo "$FIRST_MSG" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

# Try to delete it as User2 (should fail)
echo "Attempting to delete User1's message as User2 (should be forbidden):"
curl -s -X DELETE "$BASE_URL/api/conversations/$CONVERSATION_ID/messages/$FIRST_MSG_ID" \
  -H "Authorization: Bearer $USER2_TOKEN" \
  -H "Content-Type: application/json" -w "\nStatus: %{http_code}\n"
echo ""

# ============================================
# TEST 11: Error Case - Message to Yourself
# ============================================

echo "=== Test 11: Error - Create conversation with yourself ==="
curl -s -X POST "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"participantId\": $USER1_ID}" | jq .
echo ""

# ============================================
# TEST 12: Error Case - Invalid Token
# ============================================

echo "=== Test 12: Error - Invalid authorization token ==="
curl -s -X GET "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer invalid_token_here" \
  -H "Content-Type: application/json" -w "\nStatus: %{http_code}\n"
echo ""

# ============================================
# TEST 13: Verify LastMessageAt Updated
# ============================================

echo "=== Test 13: Verify LastMessageAt updated ==="
curl -s -X GET "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" | jq '.[] | {id, lastMessageAt}'
echo ""

# ============================================
# TEST 14: User2 Gets Conversations
# ============================================

echo "=== Test 14: User2 lists conversations ==="
curl -s -X GET "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer $USER2_TOKEN" \
  -H "Content-Type: application/json" | jq .
echo ""

# ============================================
# Summary
# ============================================

echo "================================"
echo "TEST SUMMARY"
echo "================================"
echo "✅ All 14 tests completed!"
echo ""
echo "Conversation ID: $CONVERSATION_ID"
echo "User 1 ID: $USER1_ID"
echo "User 2 ID: $USER2_ID"
echo ""
echo "Key Tests Performed:"
echo "✅ Create conversation"
echo "✅ Send messages"
echo "✅ Get conversations (list)"
echo "✅ Get messages (paginated)"
echo "✅ Delete message"
echo "✅ Authorization checks"
echo "✅ Error handling"
