using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cosmonaut.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Tweetbook.Data;
using Tweetbook.Domain;
using Tweetbook.Options;

namespace Tweetbook.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameter;
        private readonly DataContext _context;

        public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings,
            TokenValidationParameters tokenValidationParameter, DataContext context)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _tokenValidationParameter = tokenValidationParameter;
            _context = context;
        }

        public async Task<AuthenticationResult> RegisterAsync(string email, string password)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return new AuthenticationResult()
                {
                    Success = false,
                    Errors = new[] {"User with this email is already exists."}
                };
            }

            var newUser = new IdentityUser()
            {
                Email = email,
                UserName = email
            };

            var createdUser = await _userManager.CreateAsync(newUser, password);
            if (!createdUser.Succeeded)
            {
                return new AuthenticationResult()
                {
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }

            return await GenerateAuthenticationResultForUser(newUser);
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new AuthenticationResult()
                {
                    Success = false,
                    Errors = new[] {"There is no such a user with this email."}
                };
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!validPassword)
            {
                return new AuthenticationResult()
                {
                    Success = false,
                    Errors = new[] {"email/password combination error."}
                };
            }

            return await GenerateAuthenticationResultForUser(user);
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var principals = GetPrincipalFromToken(token);
            if (principals == null)
                return new AuthenticationResult() {Errors = new[] {"Invalid Jwt token!"}};

            var expiryDateUnix = long.Parse(principals.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expireDateDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expireDateDateTime > DateTime.UtcNow)
                return new AuthenticationResult() {Errors = new[] {"Token has not expired yet!"}};
            
            var jti = principals.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            var storedRefreshToken =  _context.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken);
            if (storedRefreshToken == null)
            {
                return new AuthenticationResult() {Errors = new[] {"This refresh token doesn't exist!"}};
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return new AuthenticationResult() {Errors = new[] {"This refresh token has been expired!"}};
            }

            if (storedRefreshToken.Invalidated)
            {
                return new AuthenticationResult() {Errors = new[] {"This refresh token has been invalidated!"}};
            }

            if (storedRefreshToken.Used)
            {
                return new AuthenticationResult() {Errors = new[] {"This refresh token has been used!"}};
            }

            if (storedRefreshToken.JwtId != jti)
            {
                return new AuthenticationResult() {Errors = new[] {"This refresh token does not match this jwt!"}};
            }

            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(principals.Claims.Single(x => x.Type == "UserId").Value);
            return await GenerateAuthenticationResultForUser(user);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameter, out var validationToken);
                return !IsJwtWithValidSecurityTokenAlgorithm(validationToken) ? null : principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityTokenAlgorithm(SecurityToken validationToken)
        {
            return (validationToken is JwtSecurityToken jwtSecurityToken)
                   && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<AuthenticationResult> GenerateAuthenticationResultForUser(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim("UserId", user.Id)
                }),
                Expires = DateTime.Now.Add(_jwtSettings.TokenLifetime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthenticationResult()
            {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }
    }
}