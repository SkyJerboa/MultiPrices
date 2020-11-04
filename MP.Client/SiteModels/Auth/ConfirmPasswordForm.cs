namespace MP.Client.SiteModels.Auth
{
    public class ConfirmPasswordForm
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }
    }
}
