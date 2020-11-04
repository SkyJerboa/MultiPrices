using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using IdentityError = MP.Client.Common.Auth.IdentityError;

namespace MP.Client.Common.JsonResponses
{
    public class JsonErrorResult : JsonResult
    {
        public ErrorResponse Error { get; set; }

        public JsonErrorResult(string message, string description = null, int status = 400) : base(null)
        {
            Error = new ErrorResponse
            {
                Status = status,
                Message = message,
                Description = description
            };

            SetObjectAndStatus();
        }

        public JsonErrorResult(IdentityError identityError, int status = 400) : base(null)
        {
            Error = new ErrorResponse
            {
                Status = status,
                Message = identityError.Error,
                Description = identityError.Message
            };

            SetObjectAndStatus();
        }

        private void SetObjectAndStatus()
        {
            StatusCode = Error.Status;
            Value = Error;
            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }
    }
}
