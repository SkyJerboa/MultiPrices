using MP.Client.Models;
using MP.Client.SiteModels.Auth;
using MP.Core.Common.Auth;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MP.Client.Common.Auth
{
    public class UserManager
    {
        public const string CONFIRM_EMAIL_TOKEN_PURPOSE = "EmailConfirmation";
        public const string RESET_PASSWORD_TOKEN_PURPOSE = "ResetPassword";

        public IdentityResult IsValidUserForm(UserCreationForm userForm)
        {
            if (!IsValidEmail(userForm.Email))
                return new IdentityResult("Incorrect Email address");

            if (!IsCorrectUserName(userForm.UserName))
                return new IdentityResult("UserName incorrect", "UserName does not match the rules");

            IdentityResult passwordIdentity = IsValidNewPassword(userForm.Password, userForm.PasswordConfirm);
            
            return passwordIdentity;
        }

        public IdentityResult IsValidNewPassword(string password, string passwordConfirm)
        {
            if (!IsPasswordsMatch(password, passwordConfirm))
                return new IdentityResult("Passwords mismatch", "Passwords must match");

            if (!IsCorrectPassword(password))
                return new IdentityResult("Password incorrect", "Password must be longer than 3 characters");

            return IdentityResult.Success;
        }

        public User CreateNewUserInstance(UserCreationForm userForm)
        {
            return new User
            {
                UserName = userForm.UserName.ToLower(),
                Email = userForm.Email.ToLower(),
                Password = PasswordHasher.HashPassword(userForm.Password),
                SecurityToken = GenerateUserSecurityToken(),
                CreateDate = DateTime.UtcNow
            };
        }

        public string GenerateEmailToken(User user, string purpose)
        {
            byte[] token = GetSecurityTokenBytes(user.SecurityToken);
            string modifier = GetUserModifier(user.ID, purpose);

            return Rfc6238AuthenticationService
                .GenerateCode(token, modifier)
                .ToString("D6", CultureInfo.InvariantCulture);
        }

        public bool Validate(User user, string receivedToken, string purpose)
        {
            int code;
            if (!int.TryParse(receivedToken, out code))
                return false;

            byte[] securityToken = GetSecurityTokenBytes(user.SecurityToken);
            string modifier = GetUserModifier(user.ID, purpose);

            return securityToken != null 
                && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
        }

        public string GenerateUserSecurityToken() => Guid.NewGuid().ToString();

        public void ResetPassword(User user, string code, string password)
        {
            bool validationRes = Validate(user, code, RESET_PASSWORD_TOKEN_PURPOSE);
            if (!validationRes)
                return;

            user.Password = PasswordHasher.HashPassword(password);
        }

        #region CreateUserValidation
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsCorrectUserName(string userName)
        {
            return !String.IsNullOrEmpty(userName) && userName.Length > 3
                && userName.All(i => Char.IsLetterOrDigit(i) || i == '_');
        }

        private bool IsCorrectPassword(string password)
        {
            return password.Length > 3;
        }

        private bool IsPasswordsMatch(string password, string passwordConfirm)
        {
            return !String.IsNullOrEmpty(password) && !String.IsNullOrEmpty(passwordConfirm)
                && password == passwordConfirm;
        }
        #endregion

        private byte[] GetSecurityTokenBytes(string securityToken) => Encoding.Unicode.GetBytes(securityToken);

        private string GetUserModifier(int userId, string purpose)
        {
            if (userId == 0)
                throw new ArgumentNullException("User ID undefined");
            if (String.IsNullOrEmpty(purpose))
                throw new ArgumentNullException("Purpose undefined");

            return $"Totp:{purpose}:{userId}";
        }
    }
}
