using Microsoft.AspNetCore.Mvc;

namespace MP.Client.Common.JsonResponses
{
    public class JsonSuccessResult : JsonResult
    {
        public JsonSuccessResult() : base(new { Success = true }) 
        { }
    }
}
