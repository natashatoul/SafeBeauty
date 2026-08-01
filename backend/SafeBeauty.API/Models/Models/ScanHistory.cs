using Microsoft.AspNetCore.Identity;

namespace SafeBeauty.API.Models;

public class ScanHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? ProductName { get; set; }
    public string IngredientJson { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }

    public IdentityUser User { get; set; } = null!;
}