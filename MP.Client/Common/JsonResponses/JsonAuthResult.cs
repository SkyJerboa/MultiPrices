using Microsoft.AspNetCore.Mvc;
using MP.Client.Models;
using MP.Client.SiteModels.Auth;
using System;

namespace MP.Client.Common.JsonResponses
{
    public class JsonAuthResult : JsonResult
    {
        public JsonAuthResult(User user, string token) : base(null)
        {
            Value = new AuthResponse
            {
                UserID = user.ID,
                Token = token
            };
        }
    }
}
