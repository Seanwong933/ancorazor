#region

using Blog.API.Common;
using Blog.API.Messages;
using Blog.API.Messages.Users;
using Blog.Entity;
using Blog.Repository;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Siegrain.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

#endregion

namespace Blog.API.Controllers
{
    [ValidateAntiForgeryToken]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAntiforgery _antiforgery;
        private readonly IConfiguration _configuration;
        private readonly IUsersRepository _repository;
        private readonly IRoleRepository _roleRepository;

        public UsersController(IUsersRepository repository, IConfiguration configuration,
            IRoleRepository roleRepository, IAntiforgery antiforgery)
        {
            _repository = repository;
            _configuration = configuration;
            _roleRepository = roleRepository;
            _antiforgery = antiforgery;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Token")]
        public async Task<IActionResult> GetToken([FromQuery] AuthUserParameter parameter)
        {
            var user = await _repository.GetByLoginNameAsync(parameter.LoginName);
            if (user == null || !SecurePasswordHasher.Verify(parameter.Password, user.Password)) return Forbid();

            var rehashTask = PasswordRehashAsync(user.Id, parameter.Password);
            var tokenTask = GenerateJwtTokenAsync(user);
            await Task.WhenAll(rehashTask, tokenTask);

            // clear credentials
            user.LoginName = null;
            user.Password = null;

            Response.Cookies.Append("access_token", tokenTask.Result.Item1,
                new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });

            return Ok(new ResponseMessage<object>
            {
                Data = new
                {
                    expires = tokenTask.Result.Item2,
                    user
                }
            });
        }

        [AllowAnonymous]
        [HttpGet("SignIn")]
        public async Task<IActionResult> SignIn([FromQuery] AuthUserParameter parameter)
        {
            var user = await _repository.GetByLoginNameAsync(parameter.LoginName);
            if (user == null || !SecurePasswordHasher.Verify(parameter.Password, user.Password)) return Forbid();

            var rehashTask = PasswordRehashAsync(user.Id, parameter.Password);

            var roles = await _roleRepository.GetRolesByUserAsync(user.Id);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.LoginName),
                new Claim(nameof(Users.AuthUpdatedAt), user.UpdatedAt.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Name)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
                });

            // clear credentials
            user.LoginName = null;
            user.Password = null;

            return Ok(new ResponseMessage<object>
            {
                Data = new
                {
                    user
                }
            });
        }

        [HttpGet("SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new ResponseMessage<object>());
        }

        /// <summary>
        /// �� SPA ��ʼ����ƾ�ݸ���ʱˢ�� XSRF Token
        ///
        /// MARK: XSRFToken refresh Ϊʲô��ȡ�ӿڷ���ˢ�£�
        /// 1. ��Ϊ��ϣ�� SSR ʱ����ƾ�ݣ�ǰ��Ҳ��Ҫ���ܶ���ݴ���
        /// 2. XSRF Token �޷����õ���ҳ�ϣ�����Ҳ�� SSR �Ĺ���Ҫ����ֻ�ܰ� Cookie ���� main.js �ϣ��ǳ����졣
        /// 3. �������볢�����������ʱ�ж�һ�������ƾ���Զ��� XSRFToken Cookie Append �� Request.Cookie �ϣ����� Append ���� .AspNetCore.Antiforgery Cookie
        /// �����ػ�ʵ�����ˡ�
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet]
        [Route("XSRFToken")]
        public IActionResult GetXSRFToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken,
                new CookieOptions() { HttpOnly = false });
            return Ok(new ResponseMessage<object>());
        }

        /// <summary>
        /// ��������
        ///
        /// Mark: Password hashing
        /// 1. ǰ���� ����+�û���+���� �� PBKDF2 ��ϣֵ���ݵ����
        /// 2. ��˼������ CSPRNG �������Σ�ƴ�������ϣ���� PBKDF2 �ٹ�ϣһ��
        /// 3. �����û����������ϣ
        /// 4. ÿ���û���¼��ҲҪ����һ�ι�ϣֵ
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("Reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordParameter parameter)
        {
            var user = await _repository.GetByIdAsync(parameter.Id);
            if (user == null || !SecurePasswordHasher.Verify(parameter.Password, user.Password))
                return Forbid();

            return Ok(new ResponseMessage<object>
            {
                Succeed = await PasswordRehashAsync(parameter.Id, parameter.NewPassword)
            });
        }

        private async Task<Tuple<string, DateTime>> GenerateJwtTokenAsync(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.LoginName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var roles = await _roleRepository.GetRolesByUserAsync(user.Id);

            claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Name)));

            var rsa = RSACryptography.CreateRsaFromPrivateKey(Constants.RSAForToken.PrivateKey);
            var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256Signature);

            var jwtSettings = _configuration.GetSection("Jwt");
            var expires = DateTime.Now.AddDays(Convert.ToDouble(jwtSettings["JwtExpireDays"]));
            var token = new JwtSecurityToken(
                jwtSettings["JwtIssuer"],
                jwtSettings["JwtIssuer"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new Tuple<string, DateTime>(new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private async Task<bool> PasswordRehashAsync(int id, string password)
        {
            
            return await _repository.UpdateAsync(new
            {
                Id = id,
                Password = SecurePasswordHasher.Hash(password),
                AuthUpdatedAt = DateTime.Now
            }) > 0;
        }
    }
}