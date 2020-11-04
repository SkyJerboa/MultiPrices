using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MP.Client.Common.Auth;
using MP.Client.Common.Configuration;
using MP.Client.Common.Email;
using MP.Client.Common.JsonResponses;
using MP.Client.Contexts;
using MP.Client.Models;
using MP.Client.SiteModels.Auth;
using MP.Core.Common.Auth;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PasswordVerificationResult = MP.Core.Common.Auth.PasswordVerificationResult;

namespace MP.Client.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class AccountController : Controller
    {
        #region queries
        const string USER_SELECT_QUERY_CONSTRUCTOR = @"SELECT {0} FROM ""Users"" WHERE {1} LIMIT 1";

        const string USER_UPDATE_QUERY_CONSTRUCTOR = @"UPDATE ""Users"" SET {0} WHERE {1}";

        const string CREATE_USER_QUERY = @"INSERT INTO ""Users"" 
            (""UserName"", ""Email"", ""Password"", ""SecurityToken"", ""CreateDate"") VALUES
            (@UserName, @Email, @Password, @SecurityToken, @CreateDate)
            RETURNING ""ID""";

        const string UPDATE_EMAIL_CONFIRMED_QUERY = @"UPDATE ""Users""
                SET (""EmailConfirmed"", ""AllowMailing"", ""SecurityToken"") 
                = (true, true, @SecurityToken) WHERE ""ID"" = @ID";

        const string INCREMENT_WRONG_PASSWORD_QUERY = @"UPDATE ""Users"" 
            SET ""AccessFailedCount"" = ""AccessFailedCount"" + 1 WHERE ""ID"" = {0}";

        const string RESET_WRONG_PASSWORD_QYERY = @"UPDATE ""Users""
            SET ""AccessFailedCount"" = NULL WHERE ""ID"" = {0}";

        const string SET_SECURITY_TOKEN_QUERY = @"UPDATE ""Users"" SET ""SecurityToken"" = @SecurityToken
            WHERE ""ID"" = @ID";
        #endregion

        private readonly TimeSpan AUTH_SHORT_LIFE_TIME = TimeSpan.FromMinutes(10);

        private IDbConnection _connection { get; }
        private UserManager _userManager { get; }
        private AuthOptions _authOptions { get; }

        public AccountController(MainContext context)
        {
            _connection = context.Database.GetDbConnection();
            _userManager = new UserManager();
            _authOptions = SiteConfigurationManager.Config.AuthOptions;
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Login([FromBody] UserLoginForm userForm)
        {
            if (User.Identity.IsAuthenticated)
                return new JsonErrorResult("Already authenticated");

            if (String.IsNullOrEmpty(userForm.UserName) || String.IsNullOrEmpty(userForm.Password))
                return new JsonErrorResult("Username or password incorrect", "Username or password is null");

            User user = FindUserInDb(userForm.UserName, userForm.UserName, "ID", "UserName", "Password");
            if (user == null)
                return new JsonErrorResult("Username or password incorrect");

            PasswordVerificationResult verifyRes
                = PasswordHasher.VerifyHashedPassword(user.Password, userForm.Password);

            switch (verifyRes)
            {
                case PasswordVerificationResult.Failed:
                    IncrementWrongPasswordCounterAsync(user.ID);
                    return new JsonErrorResult("Username or password incorrect");
                case PasswordVerificationResult.SuccessRehashNeeded:
                    ResetWrongPasswordCounterAsync(user.ID);
                    return new JsonErrorResult("Need update password", "You must set a new password", 200);
                default:
                    ResetWrongPasswordCounterAsync(user.ID);
                    return new JsonAuthResult(user, CreateAuthToken(user));
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult CreateUser([FromBody] UserCreationForm userForm)
        {
            IdentityResult identityResult = _userManager.IsValidUserForm(userForm);
            if (!identityResult.Successful)
                return new JsonErrorResult(identityResult.Error);

            User user = FindUserInDb(userForm.UserName, userForm.Email, "ID");
            if (user != null)
            {
                return (user.UserName == userForm.UserName)
                    ? new JsonErrorResult("Same user exists", "User with the same UserName already exists")
                    : new JsonErrorResult("Same user exists", "User with the same Email already exists");
            }

            user = _userManager.CreateNewUserInstance(userForm);
            user.ID = _connection.ExecuteScalar<int>(CREATE_USER_QUERY, user);

            GenerateAndSendUserCodeAsync(user, UserManager.CONFIRM_EMAIL_TOKEN_PURPOSE);
            string token = CreateAuthToken(user, AUTH_SHORT_LIFE_TIME);

            return new JsonAuthResult(user, token);
        }

        [HttpPost]
        public IActionResult ResendEmailConfirmation()
        {
            int uid = GetUserID();
            User user = FindUserInDb(uid, "ID", "Email", "EmailConfirmed", "SecurityToken");
            if (user.EmailConfirmed)
                return new JsonErrorResult("Already Confirmed", "You email already confirmed");

            SetNewUserSecurityToken(user);
            GenerateAndSendUserCodeAsync(user, UserManager.CONFIRM_EMAIL_TOKEN_PURPOSE);

            return new JsonSuccessResult();
        }

        [HttpGet]
        public IActionResult ConfirmEmail(int uid, string code)
        {
            string purpose = UserManager.CONFIRM_EMAIL_TOKEN_PURPOSE;
            IdentityResult validateTokenResult = CheckReceivedEmailToken(
                uid: uid,
                code: code,
                purpose: purpose,
                errorIfEmailConfirmed: true);

            if (!validateTokenResult.Successful)
                return new JsonErrorResult(validateTokenResult.Error);

            string token = _userManager.GenerateUserSecurityToken();
            _connection.ExecuteAsync(UPDATE_EMAIL_CONFIRMED_QUERY, new { SecurityToken = token, ID = uid });

            return new JsonSuccessResult();
        }

        [HttpPost]
        public IActionResult RefreshToken()
        {
            User user = GetUser("ID", "UserName", "EmailConfirmed");
            string token = (user.EmailConfirmed)
                ? CreateAuthToken(user, AUTH_SHORT_LIFE_TIME)
                : CreateAuthToken(user);

            return new JsonAuthResult(user, token);
        }

        [HttpPost]
        public IActionResult ForgotPassword()
        {
            User user = GetUser("ID", "Email", "SecurityToken");

            SetNewUserSecurityToken(user);
            GenerateAndSendUserCodeAsync(user, UserManager.RESET_PASSWORD_TOKEN_PURPOSE);
            return new JsonSuccessResult();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmPassword(int uid, string code)
        {
            User user = FindUserInDb(uid, "ID", "SecurityToken");
            bool validationRes = _userManager.Validate(user, code, UserManager.RESET_PASSWORD_TOKEN_PURPOSE);
            if (!validationRes)
                return new JsonErrorResult("Token expired");

            return new JsonSuccessResult();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ConfirmPassword([FromBody] ConfirmPasswordForm passwordForm)
        {
            string purpose = UserManager.RESET_PASSWORD_TOKEN_PURPOSE;
            IdentityResult validateTokenResult =
                CheckReceivedEmailToken(passwordForm.ID, passwordForm.Code, purpose);

            if (!validateTokenResult.Successful)
                return new JsonErrorResult(validateTokenResult.Error);

            UpdateUserPassword(passwordForm.ID, passwordForm.Password, true);

            return new JsonSuccessResult();
        }

        [HttpPost]
        public IActionResult ChangePassword([FromBody] ChangePasswordForm changePasswordForm)
        {
            if (String.IsNullOrEmpty(changePasswordForm.CurrentPassword))
                return new JsonErrorResult("Password undefined", "Password is null or empty");

            IdentityResult passwordIdentity =
                _userManager.IsValidNewPassword(changePasswordForm.NewPassword, changePasswordForm.PasswordConfirm);
            if (!passwordIdentity.Successful)
                return new JsonErrorResult(passwordIdentity.Error);

            User user = GetUser("ID", "Password");

            PasswordVerificationResult verifyRes
                = PasswordHasher.VerifyHashedPassword(user.Password, changePasswordForm.CurrentPassword);

            switch (verifyRes)
            {
                case PasswordVerificationResult.Failed:
                    return new JsonErrorResult("Password incorrect");
                default:
                    UpdateUserPassword(GetUserID(), changePasswordForm.NewPassword);
                    break;
            }

            return new JsonSuccessResult();
        }

        [HttpGet]
        public IActionResult UserInfo()
        {
            User user = GetUser("ID", "UserName", "Email", "AllowMailing", "EmailConfirmed");
            UserInfo userInfo = new UserInfo
            {
                ID = user.ID,
                UserName = user.UserName,
                Email = user.Email,
                AllowMailing = user.AllowMailing,
                EmailConfirmed = user.EmailConfirmed
            };

            return new JsonResult(userInfo);
        }


        #region userGetter
        private User GetUser(params string[] columns)
        {
            if (!User.Identity.IsAuthenticated)
                throw new ArgumentNullException("User is null");

            int uid = GetUserID();
            return FindUserInDb(uid, columns);
        }

        private User FindUserInDb(int id, params string[] columns)
        {
            string condition = $@"""ID"" = {id}";
            return QueryUser(condition, columns);
        }

        private User FindUserInDb(string userName, string email, params string[] columns)
        {
            userName = userName.ToLower();
            email = email.ToLower();
            string condition = $@"""UserName"" = '{userName}' OR ""Email"" = '{email}'";
            return QueryUser(condition, columns);
        }

        private User QueryUser(string condition, params string[] columns)
        {
            if (String.IsNullOrEmpty(condition))
                throw new ArgumentNullException("Condition connot be null");

            string selectColumns = (columns.Length > 0)
                ? $@"""{String.Join(@""",""", columns)}"""
                : $@"*";
            string query = String.Format(USER_SELECT_QUERY_CONSTRUCTOR, selectColumns, condition);

            User user = _connection.QueryFirstOrDefault<User>(query);
            return user;
        }
        #endregion

        async private void IncrementWrongPasswordCounterAsync(int id)
        {
            string query = String.Format(INCREMENT_WRONG_PASSWORD_QUERY, id);
            await _connection.ExecuteAsync(query);
        }

        async private void ResetWrongPasswordCounterAsync(int id)
        {
            string query = String.Format(RESET_WRONG_PASSWORD_QYERY, id);
            await _connection.ExecuteAsync(query);
        }

        private int GetUserID()
        {
            if (!User.Identity.IsAuthenticated)
                throw new NullReferenceException("User not authenticated");

            return Int32.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sid));
        }

        private IdentityResult CheckReceivedEmailToken(
            int uid, 
            string code, 
            string purpose, 
            bool errorIfEmailConfirmed = false)
        {
            if (uid == 0 || String.IsNullOrEmpty(code))
                return new IdentityResult("Parameters undefined", "UserID or Code undefined");

            User user = FindUserInDb(uid, "ID", "SecurityToken", "EmailConfirmed");
            if (user == null)
                return new IdentityResult("User not found");

            if (errorIfEmailConfirmed && user.EmailConfirmed)
                return new IdentityResult("Email already confirmed");

            bool result = _userManager.Validate(user, code, purpose);

            if (!result)
                return new IdentityResult("Invalid token");

            return IdentityResult.Success;
        }

        private void SetNewUserSecurityToken(User user)
        {
            string securityToken = _userManager.GenerateUserSecurityToken();
            user.SecurityToken = securityToken;
            
            SetNewUserSecurityTokenAsync(user.ID, securityToken);
        }

        private async void SetNewUserSecurityTokenAsync(int id, string newToken = null)
        {
            string securityToken = newToken ?? _userManager.GenerateUserSecurityToken();
            await _connection.ExecuteAsync(SET_SECURITY_TOKEN_QUERY, new { SecurityToken = securityToken, ID = id });
        }

        private async void UpdateUserPassword(int uid, string newPassword, bool updateSecurityToken = false)
        {
            newPassword = PasswordHasher.HashPassword(newPassword);
            string values = $@"""Password"" = '{newPassword}'";
            string condition = $@"""ID"" = {uid}";
            if (updateSecurityToken)
                values += $@", ""SecurityToken"" = '{_userManager.GenerateUserSecurityToken()}'";

            string query = String.Format(USER_UPDATE_QUERY_CONSTRUCTOR, values, condition);

            await _connection.ExecuteAsync(query);

        }

        private ClaimsIdentity GetIdentity(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sid, user.ID.ToString())
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token");
            return claimsIdentity;
        }

        private async void GenerateAndSendUserCodeAsync(User user, string purpose)
        {
            var code = _userManager.GenerateEmailToken(user, purpose);
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { uid = user.ID, code },
                protocol: HttpContext.Request.Scheme);
            EmailService emailService = new EmailService();
            await emailService.SendEmailAsync(user.Email, "Confirm your account",
                $"Подтвердите регистрацию, перейдя по ссылке: <a href='{callbackUrl}'>link</a>");
        }

        private string CreateAuthToken(User user, TimeSpan? lifeTime = null)
        {
            var identity = GetIdentity(user);

            DateTime now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                    issuer: _authOptions.Issuer,
                    audience: _authOptions.Audience,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(lifeTime ?? TimeSpan.FromMinutes(_authOptions.Lifetime)),
                    signingCredentials: new SigningCredentials(_authOptions.GetSymmetricSecurityKey(), 
                        SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            return encodedJwt;
        }
    }
}
