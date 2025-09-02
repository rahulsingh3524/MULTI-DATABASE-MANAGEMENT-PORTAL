namespace GlassCodeTech_Ticketing_System_Project.Models
{
    public class LoginDetail
    {
        // These properties return constant keys for mapping or identification

        public string Id => "A";
        public string Username => "B";
        public string Email => "C";
        public string CompanyName => "D";
        public string Position => "E";
        public string Role => "F";

        //// Data properties can be separate, for holding actual values
        //public string IdValue { get; set; }
        //public string UsernameValue { get; set; }
        //public string EmailValue { get; set; }
        //public string CompanyNameValue { get; set; }
        //public string PositionValue { get; set; }
        //public string RoleValue { get; set; }
    }
    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        // Extra flag to switch to PasswordOnly view
        public bool IsPasswordOnly { get; set; }
    }


}
