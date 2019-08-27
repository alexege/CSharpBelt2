using System.ComponentModel.DataAnnotations;

namespace CSharpBelt2.Models
{
    public class LogUser
    {
        [Display(Name="Email:")]
        public string LogEmail {get; set;}
        
        [Display(Name="UserName:")]
        public string LogUserName {get; set;}

        [Display(Name="Password:")]
        public string LogPassword {get; set;}
    }
}