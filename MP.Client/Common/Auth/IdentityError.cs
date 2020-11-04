namespace MP.Client.Common.Auth
{
    public class IdentityError
    {
        public string Code { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }

        public IdentityError(string error, string message = null, string code = null)
        {
            Error = error;
            Message = message;
            Code = code;
        }
    }
}
