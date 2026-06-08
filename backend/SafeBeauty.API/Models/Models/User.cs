using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    
    public Role Role {get; set;} 

    public ICollection<ScanHistory> ScanHistory { get; set; } = new List<ScanHistory>();
}