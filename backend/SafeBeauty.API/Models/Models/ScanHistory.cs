namespace SafeBeauty.API.Models;

public class ScanHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Barcode { get; set; }
    public string? ProductName { get; set; }
    public string IngredientJson { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }

    public User User { get; set; } = null!;
}