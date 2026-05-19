# Messaging System Implementation - Document Index

## Quick Access Guide

### For Deployment
1. **Step 1:** Read `README_MESSAGING.md` for overview
2. **Step 2:** Apply migration: `dotnet ef database update`
3. **Step 3:** Run app: `dotnet run`
4. **Step 4:** Test endpoints using `CURL_TEST_COMMANDS.sh`

### For Testing
- **Complete Test Suite:** `MESSAGING_SYSTEM_TESTS.md` (30+ curl examples)
- **Automated Tests:** `CURL_TEST_COMMANDS.sh` (14 test scenarios)
- **Quick Reference:** `MESSAGING_QUICK_REFERENCE.md` (endpoint summary)

### For Integration
- **Frontend Guide:** `README_MESSAGING.md` → "Integration with Frontend" section
- **API Specification:** `MESSAGING_SYSTEM_TESTS.md` → "API Endpoints" section
- **Error Handling:** `MESSAGING_SYSTEM_TESTS.md` → "Error Responses" section

---

## Document Descriptions

### Core Documentation

#### `README_MESSAGING.md`
**What:** Complete overview and quick start guide  
**When:** Start here for deployment overview  
**Contains:**
- What was implemented
- Files changed summary
- Database schema
- Quick API examples
- Frontend integration code
- Troubleshooting guide

#### `MESSAGING_SYSTEM_TESTS.md`
**What:** Detailed testing guide with 30+ curl examples  
**When:** Use for comprehensive testing and integration  
**Contains:**
- Complete API reference (all 5 endpoints)
- Request/response examples
- Step-by-step workflow test
- Error scenarios and responses
- Setup instructions
- Validation rules

#### `MESSAGING_QUICK_REFERENCE.md`
**What:** Quick reference for developers  
**When:** Quick lookup during development  
**Contains:**
- Endpoint summary table
- Database schema (text version)
- Implementation details
- Security features checklist
- Performance optimizations
- File reference guide

#### `CURL_TEST_COMMANDS.sh`
**What:** Bash script with automated tests  
**When:** Ready-to-run test suite  
**Contains:**
- 14 test scenarios
- Auto-registers test users
- Tests all 5 endpoints
- Error handling verification
- Summary report

---

## Implementation Files

### Source Code Files

#### `Models/User.cs` (Modified)
**Changes:**
- Added `Conversation` class
- Added `Message` class
- Added navigation properties to User

**Code:**
```csharp
public class Conversation {
    public int Id { get; set; }
    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
}

public class Message {
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
```

**Location:** `backend/Perspektif.API/Models/User.cs`

#### `Data/AppDbContext.cs` (Modified)
**Changes:**
- Added `DbSet<Conversation>` and `DbSet<Message>`
- Configured relationships and indexes
- Added unique constraint on (User1Id, User2Id)

**Added:**
```csharp
public DbSet<Conversation> Conversations => Set<Conversation>();
public DbSet<Message> Messages => Set<Message>();
```

**Location:** `backend/Perspektif.API/Data/AppDbContext.cs`

#### `Controllers/MessagesController.cs` (New)
**Contains:**
- 5 API endpoints
- JWT authentication
- Authorization logic
- Pagination implementation
- Error handling

**Location:** `backend/Perspektif.API/Controllers/MessagesController.cs`

#### `DTOs/AuthDtos.cs` (Modified)
**Added Records:**
- `ConversationDto`
- `MessageDto`
- `CreateConversationRequest`
- `SendMessageRequest`

**Location:** `backend/Perspektif.API/DTOs/AuthDtos.cs`

#### `Migrations/20260513175009_AddMessagingSystem.cs` (Generated)
**Creates:**
- Conversations table
- Messages table
- 7 indexes

**Location:** `backend/Perspektif.API/Migrations/20260513175009_AddMessagingSystem.cs`

---

## Database Reference

### Tables

#### Conversations
```sql
CREATE TABLE Conversations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    User1Id INTEGER NOT NULL FOREIGN KEY,
    User2Id INTEGER NOT NULL FOREIGN KEY,
    CreatedAt TEXT NOT NULL,
    LastMessageAt TEXT NOT NULL
);
```

**Indexes:**
- `IX_Conversations_User1_User2` (UNIQUE)
- `IX_Conversations_LastMessageAt`

#### Messages
```sql
CREATE TABLE Messages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Content TEXT NOT NULL,
    SenderId INTEGER NOT NULL FOREIGN KEY,
    ConversationId INTEGER NOT NULL FOREIGN KEY,
    CreatedAt TEXT NOT NULL
);
```

**Indexes:**
- `IX_Messages_ConversationId`
- `IX_Messages_CreatedAt`
- `IX_Messages_SenderId`

---

## API Endpoints Summary

| Method | Path | Purpose | Auth |
|--------|------|---------|------|
| GET | `/api/conversations` | List conversations | JWT |
| POST | `/api/conversations` | Create conversation | JWT |
| GET | `/api/conversations/{id}/messages` | Get messages | JWT |
| POST | `/api/conversations/{id}/messages` | Send message | JWT |
| DELETE | `/api/conversations/{id}/messages/{id}` | Delete message | JWT |

---

## Deployment Checklist

- [ ] Read `README_MESSAGING.md`
- [ ] Apply migration: `dotnet ef database update`
- [ ] Build project: `dotnet build` (should have 0 errors)
- [ ] Run application: `dotnet run`
- [ ] Test with `CURL_TEST_COMMANDS.sh`
- [ ] Verify all 5 endpoints working
- [ ] Check database tables created
- [ ] Test JWT authentication
- [ ] Test pagination
- [ ] Test authorization (403 Forbidden cases)
- [ ] Review logs for errors
- [ ] Deploy to production

---

## Testing Checklist

- [ ] GET /api/conversations works
- [ ] POST /api/conversations creates conversation
- [ ] POST /api/conversations prevents self-conversation
- [ ] POST /api/conversations prevents duplicates
- [ ] GET /api/conversations/{id}/messages returns paginated results
- [ ] GET /api/conversations/{id}/messages handles page parameters
- [ ] POST /api/conversations/{id}/messages sends message
- [ ] POST /api/conversations/{id}/messages updates LastMessageAt
- [ ] DELETE /api/conversations/{id}/messages/{id} deletes message
- [ ] DELETE only works for sender (403 for others)
- [ ] JWT authentication required on all endpoints
- [ ] 404 for non-existent conversation/message
- [ ] Pagination returns hasNextPage correctly
- [ ] Error messages are informative

---

## Quick Commands

### Apply Migration
```bash
cd backend/Perspektif.API
dotnet ef database update
```

### Build Project
```bash
dotnet build
```

### Run Application
```bash
dotnet run
# Listens on http://localhost:5000
```

### Run Tests
```bash
bash backend/CURL_TEST_COMMANDS.sh
```

### Clean Build
```bash
dotnet clean
dotnet build
```

---

## Troubleshooting Quick Links

**Issue:** Migration fails  
**Solution:** See `README_MESSAGING.md` → Troubleshooting → "Migration Failed"

**Issue:** Build errors  
**Solution:** See `README_MESSAGING.md` → Troubleshooting → "Build Errors"

**Issue:** JWT token issues  
**Solution:** See `README_MESSAGING.md` → Troubleshooting → "JWT Token Issues"

**Issue:** 403 Forbidden  
**Solution:** Check authorization in `MESSAGING_SYSTEM_TESTS.md` → "Error Responses"

**Issue:** 404 Not Found  
**Solution:** Verify conversation ID and ensure user is participant

---

## Integration Steps for Frontend

### Step 1: Get Conversations
```javascript
const response = await fetch('http://localhost:5000/api/conversations', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const conversations = await response.json();
```

### Step 2: Create Conversation
```javascript
await fetch('http://localhost:5000/api/conversations', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ participantId: 2 })
});
```

### Step 3: Get Messages
```javascript
const response = await fetch(
  `http://localhost:5000/api/conversations/${conversationId}/messages?page=1&pageSize=10`,
  { headers: { 'Authorization': `Bearer ${token}` } }
);
const { items, hasNextPage, page, pageSize } = await response.json();
```

### Step 4: Send Message
```javascript
await fetch(`http://localhost:5000/api/conversations/${conversationId}/messages`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ content: 'Hello!' })
});
```

### Step 5: Delete Message
```javascript
await fetch(
  `http://localhost:5000/api/conversations/${conversationId}/messages/${messageId}`,
  {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  }
);
```

---

## File Locations

**Backend Directory:** `backend/Perspektif.API/`

**Source Files:**
- `Models/User.cs`
- `Data/AppDbContext.cs`
- `Controllers/MessagesController.cs`
- `DTOs/AuthDtos.cs`
- `Migrations/20260513175009_AddMessagingSystem.cs`

**Documentation:**
- `README_MESSAGING.md`
- `MESSAGING_SYSTEM_TESTS.md`
- `MESSAGING_QUICK_REFERENCE.md`
- `CURL_TEST_COMMANDS.sh`
- `IMPLEMENTATION_INDEX.md` (this file)

---

## Version Information

- **Implementation Date:** May 13, 2026
- **Framework:** ASP.NET Core 9
- **Database:** SQLite
- **Migration:** 20260513175009_AddMessagingSystem

---

## Status

✅ Implementation Complete
✅ Tests Provided
✅ Documentation Complete
✅ Ready for Deployment

---

## Support Documents

For different needs, use:

- **Overview:** `README_MESSAGING.md`
- **Testing:** `MESSAGING_SYSTEM_TESTS.md`
- **Reference:** `MESSAGING_QUICK_REFERENCE.md`
- **Automation:** `CURL_TEST_COMMANDS.sh`
- **Index:** `IMPLEMENTATION_INDEX.md` (this file)

---

**Last Updated:** May 13, 2026  
**Status:** Production Ready
