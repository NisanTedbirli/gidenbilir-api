using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perspektif.API.Data;
using Perspektif.API.DTOs;
using Perspektif.API.Models;

namespace Perspektif.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController(AppDbContext db) : ControllerBase
{
    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/conversations — kullanıcının konuşmaları listele
    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetCurrentUserId();
        var conversations = await db.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Include(c => c.User1)
                .ThenInclude(u => u.Nationality)
            .Include(c => c.User2)
                .ThenInclude(u => u.Nationality)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var result = conversations
            .Select(c =>
            {
                var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                return new ConversationDto(
                    c.Id,
                    c.User1Id == userId ? c.User2Id : c.User1Id,
                    c.User1Id == userId ? c.User2.FullName : c.User1.FullName,
                    c.User1Id == userId ? c.User2.Nationality.FlagEmoji : c.User1.Nationality.FlagEmoji,
                    lastMsg?.Content ?? "Konuşma başladı",
                    c.LastMessageAt,
                    lastMsg?.SenderId
                );
            })
            .ToList();

        return Ok(result);
    }

    // POST /api/conversations — yeni konuşma başlat
    [HttpPost]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest req)
    {
        var userId = GetCurrentUserId();

        if (req.ParticipantId == userId)
            return BadRequest(new { message = "Kendinizle konuşma başlatamazsınız." });

        var participant = await db.Users.FirstOrDefaultAsync(u => u.Id == req.ParticipantId);
        if (participant == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        // Var olan konuşmayı kontrol et
        var existing = await db.Conversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == userId && c.User2Id == req.ParticipantId) ||
                (c.User1Id == req.ParticipantId && c.User2Id == userId));

        if (existing != null)
            return Ok(new { id = existing.Id, message = "Konuşma zaten var." });

        var conversation = new Conversation
        {
            User1Id = Math.Min(userId, req.ParticipantId),
            User2Id = Math.Max(userId, req.ParticipantId)
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMessages), new { id = conversation.Id },
            new { id = conversation.Id });
    }

    // GET /api/conversations/{id}/messages — konuşmadaki mesajları getir (paginated)
    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return NotFound(new { message = "Konuşma bulunamadı." });

        if (conversation.User1Id != userId && conversation.User2Id != userId)
            return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        // Determine if user is User1 or User2
        var isUser1 = conversation.User1Id == userId;

        // Count total non-deleted messages for this user
        var totalCount = await db.Messages
            .Where(m => m.ConversationId == id)
            .Where(m => isUser1 ? !m.IsDeletedByUser1 : !m.IsDeletedByUser2)
            .CountAsync();

        var messages = await db.Messages
            .Where(m => m.ConversationId == id)
            .Where(m => isUser1 ? !m.IsDeletedByUser1 : !m.IsDeletedByUser2)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.SenderId,
                m.Sender.FullName,
                m.CreatedAt
            ))
            .ToListAsync();

        // Reverse to show oldest first
        messages.Reverse();

        return Ok(new PagedResult<MessageDto>(
            messages,
            totalCount,
            page,
            pageSize,
            (page * pageSize) < totalCount
        ));
    }

    // POST /api/conversations/{id}/messages — mesaj gönder
    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageRequest req)
    {
        var userId = GetCurrentUserId();
        var conversation = await db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return NotFound(new { message = "Konuşma bulunamadı." });

        if (conversation.User1Id != userId && conversation.User2Id != userId)
            return Forbid();

        var message = new Message
        {
            Content = req.Content.Trim(),
            SenderId = userId,
            ConversationId = id
        };

        db.Messages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var sender = await db.Users.FirstAsync(u => u.Id == userId);

        return CreatedAtAction(nameof(GetMessages), new { id },
            new MessageDto(message.Id, message.Content, message.SenderId, sender.FullName, message.CreatedAt));
    }

    // DELETE /api/conversations/{id}/messages/{messageId} — mesaj sil (soft delete)
    [HttpDelete("{id}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int id, int messageId)
    {
        var userId = GetCurrentUserId();
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && (c.User1Id == userId || c.User2Id == userId));

        if (conversation == null)
            return NotFound(new { message = "Konuşma bulunamadı." });

        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == id);

        if (message == null)
            return NotFound(new { message = "Mesaj bulunamadı." });

        // Mark as deleted for the current user
        if (conversation.User1Id == userId)
            message.IsDeletedByUser1 = true;
        else
            message.IsDeletedByUser2 = true;

        // If both users deleted the message, hard delete it
        if (message.IsDeletedByUser1 && message.IsDeletedByUser2)
        {
            db.Messages.Remove(message);
        }
        else
        {
            db.Messages.Update(message);
        }

        await db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
