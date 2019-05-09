#region

using Blog.API.Messages.Users;
using Blog.Entity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Siegrain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blog.API.Controllers.Base;
using Blog.Service;

#endregion

namespace Blog.API.Controllers
{
    [ValidateAntiForgeryToken]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : SGControllerBase
    {
        private readonly IAntiforgery _antiforgery;
        private readonly UserService _service;
        private readonly RoleService _roleService;

        public UsersController(IAntiforgery antiforgery, UserService service, RoleService roleService) : base(service)
        {
            _antiforgery = antiforgery;
            _service = service;
            _roleService = roleService;
        }

        [AllowAnonymous]
        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn([FromBody] AuthUserParameter parameter)
        {
            var user = await _service.GetByLoginNameAsync(parameter.LoginName);
            if (user == null || !SecurePasswordHasher.Verify(parameter.Password, user.Password))
                return Forbid();

            if (!await _service.PasswordRehashAsync(user, parameter.Password))
                return NotFound("Password rehash failed.");

            await SignIn(user);

            // clear credentials
            user.LoginName = null;
            user.Password = null;
            return Ok(new { user });
        }

        [HttpPost("SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        /// <summary>
        /// �� SPA ��ʼ����ƾ�ݸ���ʱˢ�� XSRF Token
        ///
        /// MARK: XSRFToken refresh Ϊʲô��ȡ�ӿڷ���ˢ�£�
        /// 
        /// TODO: 
        ///     ����������������SignIn��SignOut�ӿڵĽ�β������һ������ӿڣ�ֱ�Ӱ��µ�XSRFToken����ȥ��
        ///     ��¼�����Header, Set-Cookie����Cookie��ȡ����ֱ�Ӹ��ӵ�Header����ȥȻ���������ӿڡ�
        ///     ͬ�������ע���ӿں����Header�ж�Ӧcookie���ɡ�
        ///     ��Cookie���ں���ô�죿��������AuthenticationEvents�����ж���
        /// 
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
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions() { HttpOnly = false });
            return Ok();
        }

        /// <summary>
        /// ��������
        ///
        /// MARK: Password hashing
        /// 1. ǰ���� ����+�û��� �� PBKDF2 ��ϣֵ���ݵ����
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
            var user = await GetCurrentUser();
            if (user == null ||
                user.Id != parameter.Id ||
                !SecurePasswordHasher.Verify(parameter.Password, user.Password))
                return Forbid();

            var rehashTask = _service.PasswordRehashAsync(user, parameter.NewPassword, true);
            await Task.WhenAll(SignOut(), rehashTask);
            return Ok(succeed: rehashTask.Result);
        }

        #region Private Methods

        private async Task SignIn(Users user)
        {
            var roles = await _roleService.GetByUserAsync(user.Id);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.LoginName),
                new Claim(nameof(Users.AuthUpdatedAt), user.AuthUpdatedAt.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Name)));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }

        #endregion

        #region Deprecated

        //[Obsolete("Jwt authentication is deprecated")]
        //[AllowAnonymous]
        //[HttpGet]
        //[Route("Token")]
        //private async Task<IActionResult> GetToken([FromQuery] AuthUserParameter parameter)
        //{
        //    var user = await _repository.GetByLoginNameAsync(parameter.LoginName);
        //    if (user == null || !SecurePasswordHasher.Verify(parameter.Password, user.Password)) return Forbid();

        //    var rehashTask = PasswordRehashAsync(user.Id, parameter.Password);
        //    var tokenTask = GenerateJwtTokenAsync(user);
        //    await Task.WhenAll(rehashTask, tokenTask);

        //    // clear credentials
        //    user.LoginName = null;
        //    user.Password = null;

        //    Response.Cookies.Append("access_token", tokenTask.Result.token,
        //        new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });

        //    return Ok(new { tokenTask.Result.expires, user });
        //}

        //[Obsolete("Jwt authentication is deprecated")]
        //private async Task<(string token, DateTime expires)> GenerateJwtTokenAsync(Users user)
        //{
        //    var claims = new List<Claim>
        //    {
        //        new Claim(JwtRegisteredClaimNames.Sub, user.LoginName),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        //    };

        //    var roles = await _roleRepository.GetRolesByUserAsync(user.Id);

        //    claims.AddRange(roles.Select(role => new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Name)));

        //    var rsa = RSACryptography.CreateRsaFromPrivateKey(Constants.RSAForToken.PrivateKey);
        //    var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256Signature);

        //    var jwtSettings = _configuration.GetSection("Jwt");
        //    var expires = DateTime.Now.AddDays(Convert.ToDouble(jwtSettings["JwtExpireDays"]));
        //    var token = new JwtSecurityToken(
        //        jwtSettings["JwtIssuer"],
        //        jwtSettings["JwtIssuer"],
        //        claims,
        //        expires: expires,
        //        signingCredentials: creds
        //    );

        //    return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        //}
        #endregion
    }
}