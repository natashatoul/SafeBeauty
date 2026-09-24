using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SafeBeauty.API.Authentication;
using SafeBeauty.API.Models;
using SafeBeauty.API.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SafeBeauty.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, EmailService emailService, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(AuthModel model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Generate an email verification token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Create the verification link
                var verificationLink = Url.Action("VerifyEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                // Send the verification email
                var emailSubject = "Email Verification";
                var emailBody = $"Please verify your email by clicking the following link: {verificationLink}";
                try
                {
                    _emailService.SendEmail(user.Email, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email sending failed: {ex.Message}");
                }

                return Ok("User registered successfully. An email verification link has been sent.");
            }

            return BadRequest(result.Errors);
        }


        // Add an action to handle email verification
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                return Ok("Email verification successful.");
            }

            return BadRequest("Email verification failed.");
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("Invalid login attempt.");
            }

            // Validate Identity credentials without creating its separate
            // application cookie; this API authenticates with the JWT cookie.
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var expiresAt = DateTimeOffset.UtcNow.AddHours(GetJwtExpireHours());
                var token = GenerateJwtToken(user, expiresAt.UtcDateTime);
                Response.Cookies.Append(
                    AuthConstants.CookieName,
                    token,
                    BuildAuthCookieOptions(expiresAt));

                return Ok();
            }

            return Unauthorized("Invalid login attempt.");
        }

        [Authorize]
        [HttpGet("session")]
        public IActionResult Session()
        {
            return Ok();
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AuthConstants.CookieName, BuildAuthCookieOptions());
            return Ok();
        }

        private double GetJwtExpireHours() =>
            Convert.ToDouble(_configuration["Jwt:ExpireHours"]);

        private CookieOptions BuildAuthCookieOptions(DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = expires,
                IsEssential = true,
                Path = "/"
            };
        }

        private string GenerateJwtToken(IdentityUser user, DateTime expiresAt)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key configuration is missing.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Issuer"],
                claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

}
