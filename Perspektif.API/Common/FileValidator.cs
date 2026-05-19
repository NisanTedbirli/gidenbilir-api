namespace Perspektif.API.Common;

public static class FileValidator
{
    private const long MaxFileSize = 10_000_000; // 10 MB

    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        ["image/jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/jpg"]  = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        ["image/png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        ["image/webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
        ["image/gif"]  = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
    };

    public static async Task<bool> IsValidImageAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxFileSize) return false;

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!Signatures.TryGetValue(contentType, out var sigs)) return false;

        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        stream.Position = 0;

        if (read < 4) return false;
        return sigs.Any(sig => header.Take(sig.Length).SequenceEqual(sig));
    }
}
