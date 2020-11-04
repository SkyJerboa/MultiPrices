namespace MP.Client.SiteModels.Auth
{
    public class UserCreationForm : UserLoginForm
    {
        public string Email { get; set; }
        public string PasswordConfirm { get; set; }
    }
}
