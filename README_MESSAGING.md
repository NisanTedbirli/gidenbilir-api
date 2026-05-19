# Messaging System Implementation - README

## Project: Giden Bilir Backend (ASP.NET Core 9)

### Status: ✅ COMPLETE

---

## What Was Implemented

A complete two-way messaging/conversation system with:

### 5 API Endpoints
1. **GET /api/conversations** — List user's conversations (ordered by latest message)
2. **POST /api/conversations** — Start new conversation with another user
3. **GET /api/conversations/{id}/messages** — Get messages (paginated, 10 per page)
4. **POST /api/conversations/{id}/messages** — Send message
5. **DELETE /api/conversations/{id}/messages/{messageId}** — Delete message (sender only)

### Database Schema
- **Conversations Table** — Two-way conversation between User1 and User2
- **Messages Table** — Individual messages within conversations
- **7 Optimized Indexes** — For performance and uniqueness constraints

### Authentication & Security
- JWT Bearer token required on all endpoints
- User isolation enforced (can't see other users' conversations)
- Sender-only delete authorization
- Input validation on all requests

### Pagination
- Default 10 messages per page
- Query parameters: `page` (1+), `pageSize` (1+)
- Returns `hasNextPage` flag for UI scrolling

---

## Files Changed

### Modified Files
| File | Changes |
|------|---------|
| `Models/User.cs` | Added Conversation & Message model classes, User navigation properties |
| `Data/AppDbContext.cs` | Added DbSet<Conversation> and DbSet<Message>, configured relationships |
| `DTOs/AuthDtos.cs` | Added ConversationDto, MessageDto, CreateConversationRequest, SendMessageRequest |

### New Files
| File | Purpose |
|------|---------|
| `Controllers/MessagesController.cs` | All 5 API endpoints |
| `Migrations/20260513175009_AddMessagingSystem.cs` | Database schema migration |

---

## Quick Start

### 1. Apply Migration
```bash
cd backend/Perspektif.API
dotnet ef database update
```

### 2. Run Application
```bash
dotnet run
# Listens on http://localhost:5000
```

### 3. Test Endpoints
See test guides below or run:
```bash
bash backend/CURL_TEST_COMMANDS.sh
```

---

## Test Documentation

### Main Test Guide
**File:** `MESSAGING_SYSTEM_TESTS.md`
- Complete endpoint reference
- 30+ curl examples with responses
- Step-by-step workflow test
- Error handling examples

### Quick Reference
**File:** `MESSAGING_QUICK_REFERENCE.md`
- Endpoint summary table
- Database schema
- Implementation details
- Security features

### Automated Test Script
**File:** `CURL_TEST_COMMANDS.sh`
- Bash script with 14 test scenarios
- Auto-registers users
- Tests all endpoints
- Verifies error handling

---

## API Examples

### Create Conversation
```bash
curl -X POST http://localhost:5000/api/conversations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"participantId": 2}'
```

### Send Message
```bash
curl -X POST http://localhost:5000/api/conversations/1/messages \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content": "Hello, how are you?"}'
```

### Get Messages (Paginated)
```bash
curl -X GET "http://localhost:5000/api/conversations/1/messages?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### List Conversations
```bash
curl -X GET http://localhost:5000/api/conversations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Delete Message
```bash
curl -X DELETE http://localhost:5000/api/conversations/1/messages/5 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Database Schema

### Conversations
```sql
CREATE TABLE Conversations (
    Id INTEGER PRIMARY KEY,
    User1Id INTEGER NOT NULL FOREIGN KEY,
    User2Id INTEGER NOT NULL FOREIGN KEY,
    CreatedAt TEXT NOT NULL,
    LastMessageAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IX_Conversations_User1_User2 ON Conversations(User1Id, User2Id);
CREATE INDEX IX_Conversations_LastMessageAt ON Conversations(LastMessageAt);
```

### Messages
```sql
CREATE TABLE Messages (
    Id INTEGER PRIMARY KEY,
    Content TEXT NOT NULL,
    SenderId INTEGER NOT NULL FOREIGN KEY,
    ConversationId INTEGER NOT NULL FOREIGN KEY,
    CreatedAt TEXT NOT NULL
);

CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
CREATE INDEX IX_Messages_CreatedAt ON Messages(CreatedAt);
CREATE INDEX IX_Messages_SenderId ON Messages(SenderId);
```

---

## Key Features

✅ **Two-way messaging** — Users can message each other bidirectionally  
✅ **Unique conversations** — Prevents duplicate conversations with same user  
✅ **Pagination** — Efficient message retrieval with 10 per page default  
✅ **Sorting** — Conversations ordered by latest message  
✅ **Authentication** — JWT Bearer token required  
✅ **Authorization** — User isolation and sender-only delete  
✅ **Timestamps** — UTC timestamps for all records  
✅ **Indexes** — Optimized for common queries  
✅ **Validation** — Input validation on all requests  
✅ **Error Handling** — Appropriate HTTP status codes  

---

## Performance Considerations

- **Unique Index** — Prevents duplicate conversations
- **LastMessageAt Index** — Fast conversation list sorting
- **Pagination** — Prevents loading entire conversations
- **Normalized Pairs** — User1Id < User2Id ordering prevents FK issues
- **Cascade Delete** — Messages deleted when conversation deleted

---

## Security Features

✅ JWT authentication on all endpoints  
✅ User isolation (can't view other users' conversations)  
✅ Authorization (must be participant to view/message)  
✅ Sender-only delete (can't delete others' messages)  
✅ Input validation (content length, participant ID)  
✅ SQL injection prevention (EF Core)  

---

## Testing Checklist

- [x] Create conversation endpoint
- [x] List conversations endpoint
- [x] Send message endpoint
- [x] Get messages with pagination
- [x] Delete message (sender only)
- [x] Authorization enforcement
- [x] Error handling (400, 403, 404)
- [x] Database migration
- [x] Project builds
- [x] Test documentation

---

## Known Limitations & Future Enhancements

### Optional Enhancements
- [ ] Read receipts (IsRead field)
- [ ] Typing indicators (SignalR)
- [ ] Message editing (UpdatedAt field)
- [ ] Soft delete (DeletedAt field)
- [ ] Message search/filter
- [ ] Block user feature
- [ ] Real-time updates (SignalR)
- [ ] Message attachments

### Design Decisions
- **Hard Delete** — Messages hard deleted (can add soft delete if needed)
- **Two FK Fields** — Conversation.User1Id, User2Id instead of array (simpler)
- **LastMessageAt** — Updated automatically on new message
- **Pagination** — 10 per page default (frontend can adjust via query param)

---

## Integration with Frontend

### React Native (Expo) Example
```jsx
// Get conversations
const response = await fetch('http://localhost:5000/api/conversations', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const conversations = await response.json();

// Send message
await fetch(`http://localhost:5000/api/conversations/${conversationId}/messages`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ content: 'Hello!' })
});

// Get messages with pagination
const page1 = await fetch(
  `http://localhost:5000/api/conversations/${conversationId}/messages?page=1&pageSize=10`,
  { headers: { 'Authorization': `Bearer ${token}` } }
);
const { items, hasNextPage, page, pageSize } = await page1.json();
```

---

## Troubleshooting

### Migration Failed
```bash
# Check migration status
dotnet ef migrations list

# Revert if needed
dotnet ef database update PreviousMigration

# Reapply
dotnet ef database update
```

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### JWT Token Issues
- Ensure `JWT_SECRET` environment variable is set
- Token should be passed as: `Authorization: Bearer YOUR_TOKEN`
- Token expires after 30 days

---

## Files Reference

### Core Implementation
- `Models/User.cs` — Models and relationships
- `Data/AppDbContext.cs` — Database configuration
- `Controllers/MessagesController.cs` — All endpoints
- `DTOs/AuthDtos.cs` — Request/response types
- `Migrations/20260513175009_AddMessagingSystem.cs` — Database schema

### Documentation
- `MESSAGING_SYSTEM_TESTS.md` — Complete test guide (30+ examples)
- `MESSAGING_QUICK_REFERENCE.md` — Quick reference
- `CURL_TEST_COMMANDS.sh` — Automated test script
- `README_MESSAGING.md` — This file

---

## Support

For questions or issues:
1. Check `MESSAGING_SYSTEM_TESTS.md` for detailed examples
2. Review error responses in test guide
3. Check controller logs in `logs/api-.log`
4. Verify JWT token is valid

---

## Version Info

- **Framework:** ASP.NET Core 9
- **Database:** SQLite
- **ORM:** Entity Framework Core 9
- **Authentication:** JWT Bearer
- **Implementation Date:** May 13, 2026

---

**Status: ✅ READY FOR PRODUCTION**

The messaging system is fully implemented, tested, and ready for deployment. All endpoints are functional and secured with JWT authentication.
