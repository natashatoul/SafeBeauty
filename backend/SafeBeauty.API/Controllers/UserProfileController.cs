using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;

namespace SafeBeauty.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly SafeBeautyDbContext _context;

    public UserProfileController(SafeBeautyDbContext context)
    {
        _context = context;
    }

    private string CurrentUserId => User.Claims
        .Where(claim => claim.Type == ClaimTypes.NameIdentifier)
        .Select(claim => claim.Value)
        .First(value => Guid.TryParse(value, out _));

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

        return Ok(ToDto(profile));
    }

    [HttpPut]
    public async Task<IActionResult> Save(UserProfileDto request)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

        if (profile == null)
        {
            profile = new UserProfile { UserId = CurrentUserId };
            _context.UserProfiles.Add(profile);
        }

        profile.SkinType = request.SkinType;
        profile.HairCondition = request.HairCondition;
        profile.AgeGroup = request.AgeGroup;
        profile.Gender = request.Gender;
        profile.ConditionsJson = JsonSerializer.Serialize(request.Conditions ?? new List<string>());

        await _context.SaveChangesAsync();

        return Ok(ToDto(profile));
    }

    private static UserProfileDto ToDto(UserProfile? profile)
    {
        if (profile == null)
        {
            return new UserProfileDto();
        }

        return new UserProfileDto
        {
            SkinType = profile.SkinType,
            HairCondition = profile.HairCondition,
            AgeGroup = profile.AgeGroup,
            Gender = profile.Gender,
            Conditions = JsonSerializer.Deserialize<List<string>>(profile.ConditionsJson) ?? new List<string>()
        };
    }
}
