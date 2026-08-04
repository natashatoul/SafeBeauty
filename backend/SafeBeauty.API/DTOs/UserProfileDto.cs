namespace SafeBeauty.API.DTOs;

public class UserProfileDto
{
    public string? SkinType { get; set; }
    public string? HairCondition { get; set; }
    public string? AgeGroup { get; set; }
    public string? Gender { get; set; }
    public List<string> Conditions { get; set; } = new();
}
