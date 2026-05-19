namespace Perspektif.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Soft-delete: hesap kapatılınca true olur; konuşmalar/mesajlar korunur,
    // FullName "Bilinmeyen kullanıcı" olarak ayarlanır.
    public bool IsDeleted { get; set; } = false;

    public int NationalityId { get; set; }
    public Nationality Nationality { get; set; } = null!;

    public ICollection<Experience> Experiences { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser1 { get; set; } = [];
    public ICollection<Conversation> ConversationsAsUser2 { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}

public class Nationality
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // "Türk", "Alman"
    public string Code { get; set; } = string.Empty;   // "TR", "DE"
    public string FlagEmoji { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // "Almanya", "Türkiye"
    public string Code { get; set; } = string.Empty;   // "DE", "TR"
    public string FlagEmoji { get; set; } = string.Empty;

    public ICollection<Experience> Experiences { get; set; } = [];
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;   // emoji

    public ICollection<Experience> Experiences { get; set; } = [];
}

public class Experience
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Rating { get; set; }                      // 1-5
    public DateTime? VisitDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? CountryId { get; set; }
    public Country? Country { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? City { get; set; }
    public string? BudgetLevel { get; set; } // "Ucuz" | "Orta" | "Pahalı"

    public ICollection<ExperiencePhoto> Photos { get; set; } = [];
    public ICollection<ExperienceLike> Likes { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}

public class ExperienceLike
{
    public int Id { get; set; }
    public int ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Comment
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class ExperiencePhoto
{
    public int Id { get; set; }
    public string CloudinaryUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public int Order { get; set; }

    public int ExperienceId { get; set; }
    public Experience Experience { get; set; } = null!;
}

public class Conversation
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;
    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = [];
}

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeletedByUser1 { get; set; } = false;
    public bool IsDeletedByUser2 { get; set; } = false;
}

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    // SHA-256 hash; raw token DB'de TUTULMAZ
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
