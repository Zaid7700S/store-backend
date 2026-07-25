using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using store.Data;
using store.Dto;
using store.Services;
using store.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;


namespace store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(StoreDbContext context,IAuthService authService,IEmailService emailService,IConfiguration configuration) : ControllerBase
    {
        private readonly StoreDbContext _context = context;
        private readonly IAuthService _authService = authService;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Users>>> GetAllUsers()
        {
            var users = await _context.Users
                        .Select(u => new UserDto
                        {
                            id = u.Id,
                            Name = u.Name,
                            userName = u.userName,
                        })
                        .ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Users>>> GetUserById(int id)
        {
            var user =  await _context.Users
                        .Where(u => u.Id == id)
                        .Select(u => new UserDto
                        {
                            id = u.Id,
                            Name = u.Name,
                            userName = u.userName,
                        })
                        .FirstOrDefaultAsync();
            if (user is null)
                return NotFound("User Not Found");
            return Ok(user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<Users>> GetUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _context.Users
                        .Where(u => u.Id == userId)
                        .Select(u => new UserDto
                        {
                            Name = u.Name,
                            userName = u.userName,
                            ProfilePictureUrl = u.ProfilePictureUrl
                            
                        })
                        .FirstOrDefaultAsync();
            if (user is null)
                return NotFound("User Not Found");
            return Ok(user);
        }


        [HttpPost("SignUp")]
        [AllowAnonymous]
        public async Task<ActionResult<Users>> SignUp(CreateUserDto newUser)
        {
            if (newUser is null)
                return BadRequest();

            var userExists = await _context.Users.AnyAsync(x => x.userName == newUser.userName);

            if (userExists)
                return BadRequest("Username taken");

            var user = new Users
            {
                Name = newUser.Name,
                userName = newUser.userName,
                password = BCrypt.Net.BCrypt.HashPassword(newUser.password),
                Role = UserRole.Customer
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            user.password = "";

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpPut("update-user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(UpdateUserDto updated)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            user.Name = updated.Name;

            if (!string.IsNullOrWhiteSpace(updated.password))
            {
                user.password = BCrypt.Net.BCrypt.HashPassword(updated.password);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user is null)
                return NotFound();

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Login")]

        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.userName == request.userName);

            if (user is null)
                return Unauthorized("Invalid Username or Password");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.password, user.password);

            if (!isPasswordValid)
                return Unauthorized("Invalid Username or Password");

            var jwt = _authService.GenerateJwtToken(user);

            var refreshToken = _authService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new { accessToken = jwt, refreshToken = refreshToken });
        }


        [HttpPost("refresh-token")]

        public async Task<IActionResult> Refresh(TokenApiDto tokenDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.RefreshToken == tokenDto.RefreshToken);

            if (user is null)
                return BadRequest("Invalid Token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Token Expired");

            var newJwt = _authService.GenerateJwtToken(user);

            var newRefreshToken = _authService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new { accessToken = newJwt, refreshToken = newRefreshToken });

        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]

        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.userName == request.userName);

            if (user is null)
                return Ok("If User Exists,Otp has been Sent");

            var otp = new Random().Next(100000, 999999).ToString();

            user.ResetOtp = otp;
            user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(user.Email!, otp);

            return Ok(new { message = "Otp Sent"});
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]

        public async Task<IActionResult> VerifyOtp(VerifyOtpDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.userName == request.userName);

            if (user is null || user.ResetOtp != request.Otp)
                return BadRequest("Invalid code.");

            if (user.OtpExpiryTime < DateTime.UtcNow)
                return BadRequest("Code has expired. Please request a new one.");

            var resetToken = _authService.GeneratePasswordResetToken(user);


            return Ok(new { resetToken = resetToken });
        }

        [HttpPost("reset-password")]
        [Authorize]

        public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
        {
            var isResetToken = User.HasClaim(c => c.Type == "ResetToken" && c.Value == "true");

            if (!isResetToken)
                return Unauthorized("Invalid Token");

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user is null)
                return NotFound();

            user.password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            user.ResetOtp = null;
            user.OtpExpiryTime = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password Reset Successfully" });

        }

        [HttpPost("upload-profile-pic")]
        [Authorize]

        public async Task<IActionResult> UploadProfilePic(IFormFile profileImage)
        {
            if (profileImage is null || profileImage.Length == 0)
                return BadRequest("No File Uploaded");

            if (profileImage.Length > 5 * 1024 * 1024)
                return BadRequest("Maximum size is 5 mb");

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user is null)
                return NotFound();

            var fileExtension = Path.GetExtension(profileImage.FileName);
            var uniqueFileName = $"profile-pictures/{Guid.NewGuid()}{fileExtension}";

            var accountId = _configuration["R2Settings:AccountId"];
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://01fac9173f428c98f02370072104715a.r2.cloudflarestorage.com",
            };

            var s3Client = new AmazonS3Client(

                    _configuration["R2Settings:AccessKey"],
                    _configuration["R2Settings:SecretKey"],
                    config
                 );

            using var stream = profileImage.OpenReadStream();
            var putRequest = new PutObjectRequest
            {
                BucketName = _configuration["R2Settings:BucketName"],
                Key = uniqueFileName,
                InputStream = stream,
                ContentType = profileImage.ContentType,
                DisablePayloadSigning = true
            };

            await s3Client.PutObjectAsync(putRequest);

            var publicDomain = _configuration["R2Settings:PublicDomain"];
            var finalUrl = $"{publicDomain}/{uniqueFileName}";

            user.ProfilePictureUrl = finalUrl;

            await _context.SaveChangesAsync();

            return Ok(new { profilePictureUrl = finalUrl });
        }
    }

    }

