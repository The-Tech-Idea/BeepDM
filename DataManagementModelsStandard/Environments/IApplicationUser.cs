using System.Collections.Generic;
using TheTechIdea.Beep.Environments;

namespace TheTechIdea.Beep.Environments
{
    public interface IApplicationUser
    {
        bool AcceptNews { get; set; }
        string Address { get; set; }
        List<string> Addresses { get; set; }
        string City { get; set; }
        string Company { get; set; }
        string CompanyNumber { get; set; }
        string Country { get; set; }
        string Department { get; set; }
        string Email { get; set; }
        string EmailConfirmed { get; set; }
        string Fax { get; set; }
        string FaxNumber { get; set; }
        string FirstName { get; set; }
        List<string> Groups { get; set; }
        string GuidID { get; set; }
        bool IsAdmin { get; set; }
        bool IsLoggedin { get; set; }
        string LastName { get; set; }
        List<string> Licenses { get; set; }
        string LoginID { get; set; }
        string Password { get; set; }
        string PasswordConfirmed { get; set; }
        string PhoneNumber { get; set; }
        string PhoneNumberConfirmed { get; set; }
        string Position { get; set; }
        string PostalCode { get; set; }
        List<string> Privileges { get; set; }
        string Profile { get; set; }
        string Region { get; set; }
        string Url { get; set; }
        UserTypes UserType { get; set; }
    }
}