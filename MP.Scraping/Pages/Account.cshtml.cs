using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MP.Core.Common.Auth;
using MP.Scraping.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MP.Scraping.Pages
{
    public class Account : PageModel
    {
        private UserContext _context;

        [FromRoute]
        public string Action { get; set; }

        public string LoginError { get; set; } = null;

        public Account(UserContext db)
        {
            _context = db;
        }

        public void OnGet()
        {

        }

        public IActionResult OnPost([FromForm]string username, [FromForm]string password)
        {
            if (Action == "login")
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    return Page();
                }
                else
                {
                    User user = _context.Users.FirstOrDefault(i => i.UserName == username);
                    if (user == null)
                    {
                        LoginError = "User not found";
                        return Page();
                    }
                    else if (!IsPasswordsMatch(user.Password, password))
                    {
                        LoginError = "Password incorrect";
                        return Page();
                    }
                    else
                    {
                        Authenticate(user);

                        string redirectStr = HttpContext.Request.Query["ReturnUrl"].ToString();
                        if (String.IsNullOrEmpty(redirectStr))
                            redirectStr = "/";

                        return Redirect(redirectStr);
                    }
                }
            }
            else if (Action == "logout")
            {
                if (User.Identity.IsAuthenticated)
                    Signout();
            }

            return Redirect("/");
        }

        private bool IsPasswordsMatch(string hashedPassword, string comparedPassword)
        {
            PasswordVerificationResult pvr = PasswordHasher.VerifyHashedPassword(hashedPassword, comparedPassword);
            return pvr != PasswordVerificationResult.Failed;
        }

        private async void Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(10),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        private async void Signout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}