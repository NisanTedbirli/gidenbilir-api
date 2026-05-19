using System.ComponentModel.DataAnnotations;

namespace Perspektif.API.DTOs;

public record RegisterRequest(
    [Required][StringLength(100, MinimumLength = 2)] string FullName,
    [Required][EmailAddress][StringLength(200)] string Email,
    [Required][StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")] string Password,
    [Required][Range(1, int.MaxValue, ErrorMessage = "Geçerli bir milliyet seçilmelidir.")] int NationalityId);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(
    string Token,
    int UserId,
    string FullName,
    string Email,
    string NationalityCode,
    string NationalityFlag);

public record ExperienceCreateRequest(
    [Required][StringLength(200, MinimumLength = 3)] string Title,
    [Required][StringLength(5000, MinimumLength = 10)] string Description,
    [Required][Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")] int Rating,
    int? CountryId,
    int? CategoryId,
    DateTime? VisitDate,
    [StringLength(100)] string? City,
    [StringLength(20)] string? BudgetLevel);

public record ExperienceDto(
    int Id,
    string Title,
    string Description,
    int Rating,
    DateTime? VisitDate,
    DateTime CreatedAt,
    string CountryName,
    string CountryFlag,
    string CategoryName,
    string CategoryIcon,
    int AuthorId,
    string AuthorName,
    string AuthorNationality,
    string AuthorNationalityFlag,
    List<string> PhotoUrls,
    string? City,
    string? BudgetLevel,
    int LikeCount,
    bool IsLikedByMe);

public record CommentDto(
    int Id,
    string Text,
    DateTime CreatedAt,
    int AuthorId,
    string AuthorName,
    string AuthorNationalityFlag);

public record AddCommentRequest(
    [Required][StringLength(1000, MinimumLength = 1)] string Text);

public record ExperienceUpdateRequest(
    [Required][StringLength(200, MinimumLength = 3)] string Title,
    [Required][StringLength(5000, MinimumLength = 10)] string Description,
    [Required][Range(1, 5)] int Rating,
    int CategoryId);

public record LookupDto(int Id, string Name, string Extra);

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email);

public record ResetPasswordRequest(
    [Required] string Token,
    [Required][StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")] string NewPassword);

public record UpdateProfileRequest(
    [Required][StringLength(100, MinimumLength = 2)] string FullName,
    [Required][Range(1, int.MaxValue, ErrorMessage = "Geçerli bir milliyet seçilmelidir.")] int NationalityId);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni şifre en az 8 karakter olmalıdır.")] string NewPassword);

public record DeleteAccountRequest(
    [Required] string Password);

public record UserStatsDto(
    int UserId,
    string FullName,
    string Email,
    string NationalityCode,
    string NationalityFlag,
    DateTime CreatedAt,
    int ExperienceCount,
    int TotalLikes,
    double AverageRating);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasNextPage);

public record ConversationDto(
    int Id,
    int OtherUserId,
    string OtherUserName,
    string OtherUserNationalityFlag,
    string LastMessage,
    DateTime LastMessageAt,
    int? LastSenderId);

public record MessageDto(
    int Id,
    string Content,
    int SenderId,
    string SenderName,
    DateTime CreatedAt);

public record CreateConversationRequest(
    [Required][Range(1, int.MaxValue)] int ParticipantId);

public record SendMessageRequest(
    [Required][StringLength(2000, MinimumLength = 1)] string Content);
