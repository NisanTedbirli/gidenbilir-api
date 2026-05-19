using Microsoft.EntityFrameworkCore;
using Perspektif.API.Models;

namespace Perspektif.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperiencePhoto> ExperiencePhotos => Set<ExperiencePhoto>();
    public DbSet<ExperienceLike> ExperienceLikes => Set<ExperienceLike>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.NationalityId).HasDatabaseName("IX_Users_NationalityId");
            e.HasOne(u => u.Nationality).WithMany(n => n.Users).HasForeignKey(u => u.NationalityId);
        });

        mb.Entity<Experience>(e =>
        {
            e.HasOne(x => x.User).WithMany(u => u.Experiences).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Country).WithMany(c => c.Experiences).HasForeignKey(x => x.CountryId).IsRequired(false);
            e.HasOne(x => x.Category).WithMany(c => c.Experiences).HasForeignKey(x => x.CategoryId).IsRequired(false);
            // Filtreleme indeksleri
            e.HasIndex(x => x.CountryId).HasDatabaseName("IX_Experiences_CountryId");
            e.HasIndex(x => x.UserId).HasDatabaseName("IX_Experiences_UserId");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_Experiences_CreatedAt");
            e.HasIndex(x => new { x.CountryId, x.UserId }).HasDatabaseName("IX_Experiences_Country_User");
        });

        mb.Entity<ExperienceLike>(e =>
        {
            e.HasIndex(l => new { l.ExperienceId, l.UserId }).IsUnique();
            e.HasOne(l => l.Experience).WithMany(x => x.Likes).HasForeignKey(l => l.ExperienceId);
            e.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Comment>(e =>
        {
            e.HasOne(c => c.Experience).WithMany(x => x.Comments).HasForeignKey(c => c.ExperienceId);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<Conversation>(e =>
        {
            e.HasOne(c => c.User1).WithMany(u => u.ConversationsAsUser1).HasForeignKey(c => c.User1Id).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(c => c.User2).WithMany(u => u.ConversationsAsUser2).HasForeignKey(c => c.User2Id).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique().HasDatabaseName("IX_Conversations_User1_User2");
            e.HasIndex(c => c.LastMessageAt).HasDatabaseName("IX_Conversations_LastMessageAt");
        });

        mb.Entity<Message>(e =>
        {
            e.HasOne(m => m.Sender).WithMany(u => u.Messages).HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => m.ConversationId).HasDatabaseName("IX_Messages_ConversationId");
            e.HasIndex(m => m.CreatedAt).HasDatabaseName("IX_Messages_CreatedAt");
            e.HasIndex(m => m.IsDeletedByUser1).HasDatabaseName("IX_Messages_IsDeletedByUser1");
            e.HasIndex(m => m.IsDeletedByUser2).HasDatabaseName("IX_Messages_IsDeletedByUser2");
        });

        mb.Entity<PasswordResetToken>(e =>
        {
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.ExpiresAt);
        });

        // Seed: Nationalities (60 milliyet)
        mb.Entity<Nationality>().HasData(
            new Nationality { Id = 1,  Name = "Türk",           Code = "TR", FlagEmoji = "🇹🇷" },
            new Nationality { Id = 2,  Name = "Alman",          Code = "DE", FlagEmoji = "🇩🇪" },
            new Nationality { Id = 3,  Name = "Fransız",        Code = "FR", FlagEmoji = "🇫🇷" },
            new Nationality { Id = 4,  Name = "İngiliz",        Code = "GB", FlagEmoji = "🇬🇧" },
            new Nationality { Id = 5,  Name = "Amerikalı",      Code = "US", FlagEmoji = "🇺🇸" },
            new Nationality { Id = 6,  Name = "Japon",          Code = "JP", FlagEmoji = "🇯🇵" },
            new Nationality { Id = 7,  Name = "İtalyan",        Code = "IT", FlagEmoji = "🇮🇹" },
            new Nationality { Id = 8,  Name = "İspanyol",       Code = "ES", FlagEmoji = "🇪🇸" },
            new Nationality { Id = 9,  Name = "Rus",            Code = "RU", FlagEmoji = "🇷🇺" },
            new Nationality { Id = 10, Name = "Çinli",          Code = "CN", FlagEmoji = "🇨🇳" },
            new Nationality { Id = 11, Name = "Hollandalı",     Code = "NL", FlagEmoji = "🇳🇱" },
            new Nationality { Id = 12, Name = "Yunan",          Code = "GR", FlagEmoji = "🇬🇷" },
            new Nationality { Id = 13, Name = "Portekizli",     Code = "PT", FlagEmoji = "🇵🇹" },
            new Nationality { Id = 14, Name = "Avusturyalı",    Code = "AT", FlagEmoji = "🇦🇹" },
            new Nationality { Id = 15, Name = "İsviçreli",      Code = "CH", FlagEmoji = "🇨🇭" },
            new Nationality { Id = 16, Name = "Belçikalı",      Code = "BE", FlagEmoji = "🇧🇪" },
            new Nationality { Id = 17, Name = "İsveçli",        Code = "SE", FlagEmoji = "🇸🇪" },
            new Nationality { Id = 18, Name = "Norveçli",       Code = "NO", FlagEmoji = "🇳🇴" },
            new Nationality { Id = 19, Name = "Danimarkalı",    Code = "DK", FlagEmoji = "🇩🇰" },
            new Nationality { Id = 20, Name = "Polonyalı",      Code = "PL", FlagEmoji = "🇵🇱" },
            new Nationality { Id = 21, Name = "Çekli",          Code = "CZ", FlagEmoji = "🇨🇿" },
            new Nationality { Id = 22, Name = "Macar",          Code = "HU", FlagEmoji = "🇭🇺" },
            new Nationality { Id = 23, Name = "Hırvat",         Code = "HR", FlagEmoji = "🇭🇷" },
            new Nationality { Id = 24, Name = "Rumen",          Code = "RO", FlagEmoji = "🇷🇴" },
            new Nationality { Id = 25, Name = "Sırp",           Code = "RS", FlagEmoji = "🇷🇸" },
            new Nationality { Id = 26, Name = "Fin",            Code = "FI", FlagEmoji = "🇫🇮" },
            new Nationality { Id = 27, Name = "Güney Koreli",   Code = "KR", FlagEmoji = "🇰🇷" },
            new Nationality { Id = 28, Name = "Hintli",         Code = "IN", FlagEmoji = "🇮🇳" },
            new Nationality { Id = 29, Name = "Tayvanlı",       Code = "TW", FlagEmoji = "🇹🇼" },
            new Nationality { Id = 30, Name = "Taylandlı",      Code = "TH", FlagEmoji = "🇹🇭" },
            new Nationality { Id = 31, Name = "Endonezyal",     Code = "ID", FlagEmoji = "🇮🇩" },
            new Nationality { Id = 32, Name = "Singapurlu",     Code = "SG", FlagEmoji = "🇸🇬" },
            new Nationality { Id = 33, Name = "Malezyalı",      Code = "MY", FlagEmoji = "🇲🇾" },
            new Nationality { Id = 34, Name = "Vietnamlı",      Code = "VN", FlagEmoji = "🇻🇳" },
            new Nationality { Id = 35, Name = "Filipinli",      Code = "PH", FlagEmoji = "🇵🇭" },
            new Nationality { Id = 36, Name = "Nepalli",        Code = "NP", FlagEmoji = "🇳🇵" },
            new Nationality { Id = 37, Name = "Sri Lankalı",    Code = "LK", FlagEmoji = "🇱🇰" },
            new Nationality { Id = 38, Name = "Emiratli",       Code = "AE", FlagEmoji = "🇦🇪" },
            new Nationality { Id = 39, Name = "Suudi",          Code = "SA", FlagEmoji = "🇸🇦" },
            new Nationality { Id = 40, Name = "Katarlı",        Code = "QA", FlagEmoji = "🇶🇦" },
            new Nationality { Id = 41, Name = "İsrailli",       Code = "IL", FlagEmoji = "🇮🇱" },
            new Nationality { Id = 42, Name = "Mısırlı",        Code = "EG", FlagEmoji = "🇪🇬" },
            new Nationality { Id = 43, Name = "Faslı",          Code = "MA", FlagEmoji = "🇲🇦" },
            new Nationality { Id = 44, Name = "Güney Afrikalı", Code = "ZA", FlagEmoji = "🇿🇦" },
            new Nationality { Id = 45, Name = "Kenyalı",        Code = "KE", FlagEmoji = "🇰🇪" },
            new Nationality { Id = 46, Name = "Tunuslu",        Code = "TN", FlagEmoji = "🇹🇳" },
            new Nationality { Id = 47, Name = "Kanadalı",       Code = "CA", FlagEmoji = "🇨🇦" },
            new Nationality { Id = 48, Name = "Meksikalı",      Code = "MX", FlagEmoji = "🇲🇽" },
            new Nationality { Id = 49, Name = "Brezilyalı",     Code = "BR", FlagEmoji = "🇧🇷" },
            new Nationality { Id = 50, Name = "Arjantinli",     Code = "AR", FlagEmoji = "🇦🇷" },
            new Nationality { Id = 51, Name = "Kolombiyalı",    Code = "CO", FlagEmoji = "🇨🇴" },
            new Nationality { Id = 52, Name = "Perulu",         Code = "PE", FlagEmoji = "🇵🇪" },
            new Nationality { Id = 53, Name = "Kübalı",         Code = "CU", FlagEmoji = "🇨🇺" },
            new Nationality { Id = 54, Name = "Avustralyalı",   Code = "AU", FlagEmoji = "🇦🇺" },
            new Nationality { Id = 55, Name = "Yeni Zelandalı", Code = "NZ", FlagEmoji = "🇳🇿" },
            new Nationality { Id = 56, Name = "Gürcü",          Code = "GE", FlagEmoji = "🇬🇪" },
            new Nationality { Id = 57, Name = "Azerbaycanlı",   Code = "AZ", FlagEmoji = "🇦🇿" },
            new Nationality { Id = 58, Name = "Arnavut",        Code = "AL", FlagEmoji = "🇦🇱" },
            new Nationality { Id = 59, Name = "Karadağlı",      Code = "ME", FlagEmoji = "🇲🇪" },
            new Nationality { Id = 60, Name = "Hong Konglu",    Code = "HK", FlagEmoji = "🇭🇰" }
        );

        // Seed: Countries (60 ülke)
        mb.Entity<Country>().HasData(
            // Avrupa
            new Country { Id = 1,  Name = "Türkiye",        Code = "TR", FlagEmoji = "🇹🇷" },
            new Country { Id = 2,  Name = "Almanya",        Code = "DE", FlagEmoji = "🇩🇪" },
            new Country { Id = 3,  Name = "Fransa",         Code = "FR", FlagEmoji = "🇫🇷" },
            new Country { Id = 4,  Name = "İngiltere",      Code = "GB", FlagEmoji = "🇬🇧" },
            new Country { Id = 5,  Name = "İtalya",         Code = "IT", FlagEmoji = "🇮🇹" },
            new Country { Id = 6,  Name = "İspanya",        Code = "ES", FlagEmoji = "🇪🇸" },
            new Country { Id = 7,  Name = "Hollanda",       Code = "NL", FlagEmoji = "🇳🇱" },
            new Country { Id = 8,  Name = "Yunanistan",     Code = "GR", FlagEmoji = "🇬🇷" },
            new Country { Id = 9,  Name = "Portekiz",       Code = "PT", FlagEmoji = "🇵🇹" },
            new Country { Id = 10, Name = "Avusturya",      Code = "AT", FlagEmoji = "🇦🇹" },
            new Country { Id = 11, Name = "İsviçre",        Code = "CH", FlagEmoji = "🇨🇭" },
            new Country { Id = 12, Name = "Belçika",        Code = "BE", FlagEmoji = "🇧🇪" },
            new Country { Id = 13, Name = "İsveç",          Code = "SE", FlagEmoji = "🇸🇪" },
            new Country { Id = 14, Name = "Norveç",         Code = "NO", FlagEmoji = "🇳🇴" },
            new Country { Id = 15, Name = "Danimarka",      Code = "DK", FlagEmoji = "🇩🇰" },
            new Country { Id = 16, Name = "Polonya",        Code = "PL", FlagEmoji = "🇵🇱" },
            new Country { Id = 17, Name = "Çekya",          Code = "CZ", FlagEmoji = "🇨🇿" },
            new Country { Id = 18, Name = "Macaristan",     Code = "HU", FlagEmoji = "🇭🇺" },
            new Country { Id = 19, Name = "Hırvatistan",    Code = "HR", FlagEmoji = "🇭🇷" },
            new Country { Id = 20, Name = "Sırbistan",      Code = "RS", FlagEmoji = "🇷🇸" },
            new Country { Id = 21, Name = "Romanya",        Code = "RO", FlagEmoji = "🇷🇴" },
            new Country { Id = 22, Name = "Rusya",          Code = "RU", FlagEmoji = "🇷🇺" },
            new Country { Id = 23, Name = "Finlandiya",     Code = "FI", FlagEmoji = "🇫🇮" },
            // Asya
            new Country { Id = 24, Name = "Japonya",        Code = "JP", FlagEmoji = "🇯🇵" },
            new Country { Id = 25, Name = "Güney Kore",     Code = "KR", FlagEmoji = "🇰🇷" },
            new Country { Id = 26, Name = "Çin",            Code = "CN", FlagEmoji = "🇨🇳" },
            new Country { Id = 27, Name = "Hindistan",      Code = "IN", FlagEmoji = "🇮🇳" },
            new Country { Id = 28, Name = "Tayland",        Code = "TH", FlagEmoji = "🇹🇭" },
            new Country { Id = 29, Name = "Endonezya",      Code = "ID", FlagEmoji = "🇮🇩" },
            new Country { Id = 30, Name = "Singapur",       Code = "SG", FlagEmoji = "🇸🇬" },
            new Country { Id = 31, Name = "Malezya",        Code = "MY", FlagEmoji = "🇲🇾" },
            new Country { Id = 32, Name = "Vietnam",        Code = "VN", FlagEmoji = "🇻🇳" },
            new Country { Id = 33, Name = "Filipinler",     Code = "PH", FlagEmoji = "🇵🇭" },
            new Country { Id = 34, Name = "Tayvan",         Code = "TW", FlagEmoji = "🇹🇼" },
            new Country { Id = 35, Name = "Hong Kong",      Code = "HK", FlagEmoji = "🇭🇰" },
            new Country { Id = 36, Name = "Nepal",          Code = "NP", FlagEmoji = "🇳🇵" },
            new Country { Id = 37, Name = "Sri Lanka",      Code = "LK", FlagEmoji = "🇱🇰" },
            // Orta Doğu & Afrika
            new Country { Id = 38, Name = "Dubai (BAE)",    Code = "AE", FlagEmoji = "🇦🇪" },
            new Country { Id = 39, Name = "Suudi Arabistan",Code = "SA", FlagEmoji = "🇸🇦" },
            new Country { Id = 40, Name = "Katar",          Code = "QA", FlagEmoji = "🇶🇦" },
            new Country { Id = 41, Name = "İsrail",         Code = "IL", FlagEmoji = "🇮🇱" },
            new Country { Id = 42, Name = "Mısır",          Code = "EG", FlagEmoji = "🇪🇬" },
            new Country { Id = 43, Name = "Fas",            Code = "MA", FlagEmoji = "🇲🇦" },
            new Country { Id = 44, Name = "Güney Afrika",   Code = "ZA", FlagEmoji = "🇿🇦" },
            new Country { Id = 45, Name = "Kenya",          Code = "KE", FlagEmoji = "🇰🇪" },
            new Country { Id = 46, Name = "Tunus",          Code = "TN", FlagEmoji = "🇹🇳" },
            // Amerika
            new Country { Id = 47, Name = "ABD",            Code = "US", FlagEmoji = "🇺🇸" },
            new Country { Id = 48, Name = "Kanada",         Code = "CA", FlagEmoji = "🇨🇦" },
            new Country { Id = 49, Name = "Meksika",        Code = "MX", FlagEmoji = "🇲🇽" },
            new Country { Id = 50, Name = "Brezilya",       Code = "BR", FlagEmoji = "🇧🇷" },
            new Country { Id = 51, Name = "Arjantin",       Code = "AR", FlagEmoji = "🇦🇷" },
            new Country { Id = 52, Name = "Kolombiya",      Code = "CO", FlagEmoji = "🇨🇴" },
            new Country { Id = 53, Name = "Peru",           Code = "PE", FlagEmoji = "🇵🇪" },
            new Country { Id = 54, Name = "Küba",           Code = "CU", FlagEmoji = "🇨🇺" },
            // Okyanusya
            new Country { Id = 55, Name = "Avustralya",     Code = "AU", FlagEmoji = "🇦🇺" },
            new Country { Id = 56, Name = "Yeni Zelanda",   Code = "NZ", FlagEmoji = "🇳🇿" },
            // Komşular & popüler
            new Country { Id = 57, Name = "Gürcistan",      Code = "GE", FlagEmoji = "🇬🇪" },
            new Country { Id = 58, Name = "Azerbaycan",     Code = "AZ", FlagEmoji = "🇦🇿" },
            new Country { Id = 59, Name = "Arnavutluk",     Code = "AL", FlagEmoji = "🇦🇱" },
            new Country { Id = 60, Name = "Karadağ",        Code = "ME", FlagEmoji = "🇲🇪" }
        );

        // Seed: Categories
        mb.Entity<Category>().HasData(
            new Category { Id = 1,  Name = "Yemek & İçecek",        Icon = "🍽️" },
            new Category { Id = 2,  Name = "Kültür & Tarih",        Icon = "🏛️" },
            new Category { Id = 3,  Name = "Konaklama",             Icon = "🏨" },
            new Category { Id = 4,  Name = "Ulaşım",                Icon = "🚇" },
            new Category { Id = 5,  Name = "Doğa & Manzara",        Icon = "🌿" },
            new Category { Id = 6,  Name = "Gece Hayatı",           Icon = "🌃" },
            new Category { Id = 7,  Name = "Alışveriş",             Icon = "🛍️" },
            new Category { Id = 8,  Name = "Güvenlik",              Icon = "🔒" },
            new Category { Id = 9,  Name = "Dil & İletişim",        Icon = "🗣️" },
            new Category { Id = 10, Name = "Para & Bütçe",          Icon = "💰" },
            new Category { Id = 11, Name = "Hava & İklim",          Icon = "☀️" },
            new Category { Id = 12, Name = "Eğlence & Aktivite",    Icon = "🎯" },
            new Category { Id = 13, Name = "Sağlık & Hastane",      Icon = "🏥" },
            new Category { Id = 14, Name = "Vizyon & Pratik Bilgi", Icon = "📋" },
            new Category { Id = 15, Name = "Diğer",                 Icon = "💡" }
        );
    }
}
