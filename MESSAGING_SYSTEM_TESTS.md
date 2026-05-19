# Messaging/Conversation System - Test Guide

## Overview
Complete implementation of two-way messaging system for Giden Bilir backend. All endpoints require JWT authentication.

## Database Schema

### Models Created
- **Conversation** — Two users, User1Id + User2Id (unique pair), timestamps
- **Message** — Content, SenderId, ConversationId, CreatedAt
- **User Relations** — ConversationsAsUser1, ConversationsAsUser2, Messages collections

### Indexes
- `IX_Conversations_User1_User2` — Unique pair (prevents duplicate conversations)
- `IX_Conversations_LastMessageAt` — For ordering conversations
- `IX_Messages_ConversationId` — Fast message lookup
- `IX_Messages_CreatedAt` — For pagination

### Cascade Behavior
- Messages.ConversationId → Cascade delete
- User relationships → NoAction (prevent orphaned FK issues)

---

## Setup

### 1. Apply Migration
```bash
cd backend/Perspektif.API
dotnet ef database update
```

### 2. Build & Run
```bash
dotnet build
dotnet run
# API runs at http://localhost:5000
```

---

## API Endpoints

### 1. GET /api/conversations
**List all conversations for the current user (ordered by last message time)**

```bash
curl -X GET http://localhost:5000/api/conversations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "otherUserId": 2,
    "otherUserName": "Alice",
    "otherUserNationalityFlag": "🇺🇸",
    "lastMessage": "Merhaba! Nasılsın?",
    "lastMessageAt": "2026-05-13T20:50:00Z"
  },
  {
    "id": 2,
    "otherUserId": 3,
    "otherUserName": "Bob",
    "otherUserNationalityFlag": "🇬🇧",
    "lastMessage": "Yarın buluşabilir miyiz?",
    "lastMessageAt": "2026-05-13T19:30:00Z"
  }
]
```

---

### 2. POST /api/conversations
**Start a new conversation with a user**

```bash
curl -X POST http://localhost:5000/api/conversations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "participantId": 2
  }'
```

**Request Body:**
```json
{
  "participantId": 2  // Required: ID of the other user
}
```

**Responses:**
- **201 Created** — New conversation started
```json
{
  "id": 1
}
```

- **200 OK** — Conversation already exists
```json
{
  "id": 1,
  "message": "Konuşma zaten var."
}
```

- **400 Bad Request** — Can't message yourself
```json
{
  "message": "Kendinizle konuşma başlatamazsınız."
}
```

- **404 Not Found** — User doesn't exist
```json
{
  "message": "Kullanıcı bulunamadı."
}
```

---

### 3. GET /api/conversations/{id}/messages
**Get messages from a conversation (paginated, 10 per page)**

```bash
# Page 1 (default)
curl -X GET "http://localhost:5000/api/conversations/1/messages" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"

# Page 2
curl -X GET "http://localhost:5000/api/conversations/1/messages?page=2&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"

# Custom page size (20 messages)
curl -X GET "http://localhost:5000/api/conversations/1/messages?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Query Parameters:**
- `page` — Page number (default: 1, min: 1)
- `pageSize` — Messages per page (default: 10, min: 1)

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "content": "Merhaba! Nasılsın?",
      "senderId": 2,
      "senderName": "Alice",
      "createdAt": "2026-05-13T20:45:00Z"
    },
    {
      "id": 2,
      "content": "İyiyim, sen nasılsın?",
      "senderId": 1,
      "senderName": "John",
      "createdAt": "2026-05-13T20:50:00Z"
    }
  ],
  "totalCount": 25,
  "page": 1,
  "pageSize": 10,
  "hasNextPage": true
}
```

**Error Responses:**
- **404 Not Found** — Conversation not found
- **403 Forbidden** — Not a participant in this conversation

---

### 4. POST /api/conversations/{id}/messages
**Send a message in a conversation**

```bash
curl -X POST http://localhost:5000/api/conversations/1/messages \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Merhaba! Nasılsın?"
  }'
```

**Request Body:**
```json
{
  "content": "Your message here"  // Required: 1-2000 chars
}
```

**Response (201 Created):**
```json
{
  "id": 3,
  "content": "Merhaba! Nasılsın?",
  "senderId": 1,
  "senderName": "John",
  "createdAt": "2026-05-13T20:55:00Z"
}
```

**Updates Conversation.LastMessageAt timestamp automatically.**

**Error Responses:**
- **404 Not Found** — Conversation not found
- **403 Forbidden** — Not a participant
- **400 Bad Request** — Content validation failed

---

### 5. DELETE /api/conversations/{id}/messages/{messageId}
**Delete a message (only sender can delete)**

```bash
curl -X DELETE http://localhost:5000/api/conversations/1/messages/3 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Response (204 No Content)** — Success (no response body)

**Error Responses:**
- **404 Not Found** — Message or conversation not found
- **403 Forbidden** — You are not the sender

---

## Complete Test Workflow

### Step 1: Register/Login Two Users

```bash
# Register User 1
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "password": "Password123",
    "nationalityId": 1
  }'
# Save: USER1_TOKEN and USER1_ID

# Register User 2
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Alice Smith",
    "email": "alice@example.com",
    "password": "Password123",
    "nationalityId": 2
  }'
# Save: USER2_TOKEN and USER2_ID
```

### Step 2: User1 Creates Conversation with User2

```bash
export USER1_TOKEN="eyJ..."
export USER2_ID=2

curl -X POST http://localhost:5000/api/conversations \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"participantId\": $USER2_ID}"

# Response: {"id": 1}
export CONVERSATION_ID=1
```

### Step 3: User1 Sends First Message

```bash
curl -X POST http://localhost:5000/api/conversations/$CONVERSATION_ID/messages \
  -H "Authorization: Bearer $USER1_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "Merhaba! Nasılsın?"}'
```

### Step 4: User2 Reads Conversation

```bash
export USER2_TOKEN="eyJ..."

curl -X GET http://localhost:5000/api/conversations \
  -H "Authorization: Bearer $USER2_TOKEN"
# See conversation with User1
```

### Step 5: User2 Sends Reply

```bash
curl -X POST http://localhost:5000/api/conversations/$CONVERSATION_ID/messages \
  -H "Authorization: Bearer $USER2_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "İyiyim, sen nasılsın?"}'
```

### Step 6: User1 Gets Messages (Paginated)

```bash
curl -X GET "http://localhost:5000/api/conversations/$CONVERSATION_ID/messages?page=1&pageSize=5" \
  -H "Authorization: Bearer $USER1_TOKEN"
```

### Step 7: User1 Deletes Their Message

```bash
# Message ID from earlier response
export MESSAGE_ID=1

curl -X DELETE http://localhost:5000/api/conversations/$CONVERSATION_ID/messages/$MESSAGE_ID \
  -H "Authorization: Bearer $USER1_TOKEN"
# Response: 204 No Content
```

---

## Key Features

✅ **JWT Authentication** — All endpoints require valid Bearer token  
✅ **Authorization** — Users can only see/delete their own conversations/messages  
✅ **Pagination** — 10 messages per page (configurable)  
✅ **Unique Conversations** — Can't create duplicate conversation with same user  
✅ **LastMessageAt Tracking** — Conversations sorted by latest message  
✅ **Soft-friendly Design** — Hard delete (can be changed to soft delete if needed)  
✅ **Indexes** — Optimized queries for list and detail views  
✅ **Timestamps** — All records track CreatedAt in UTC  

---

## Database Migration

**File:** `Migrations/20260513175009_AddMessagingSystem.cs`

**Created Tables:**
- Conversations (Id, User1Id, User2Id, CreatedAt, LastMessageAt)
- Messages (Id, Content, SenderId, ConversationId, CreatedAt)

**Relationships:**
- Conversation → User1 (NoAction)
- Conversation → User2 (NoAction)  
- Message → Conversation (Cascade)
- Message → Sender (NoAction)

---

## Integration Notes

- Controller uses `[Authorize]` attribute — JWT token required
- `GetCurrentUserId()` extracts from ClaimTypes.NameIdentifier
- Uses `math.Min/Max` for normalized User1Id < User2Id ordering
- Pagination returns `hasNextPage` flag for infinite scroll UI
- All timestamps are UTC (DateTime.UtcNow)

---

## Files Modified/Created

- **Models:** `Models/User.cs` — Added Conversation & Message models + User relations
- **DbContext:** `Data/AppDbContext.cs` — Added DbSets + OnModelCreating config
- **Controller:** `Controllers/MessagesController.cs` — All 5 endpoints (NEW)
- **DTOs:** `DTOs/AuthDtos.cs` — Added ConversationDto, MessageDto, requests
- **Migration:** `Migrations/20260513175009_AddMessagingSystem.cs` (AUTO-GENERATED)
