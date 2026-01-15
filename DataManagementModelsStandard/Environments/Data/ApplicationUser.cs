using TheTechIdea.Beep.Environments;
using TheTechIdea.Beep.Editor;
using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Environments.UserManagement
{
    public class ApplicationUser : Entity, IApplicationUser
    {
        public ApplicationUser()
    {
        GuidID = Guid.NewGuid().ToString();
    }
    public ApplicationUser(string loginid)
    {
        GuidID = Guid.NewGuid().ToString();
        LoginID = loginid;
    }

    private string _loginid;
    public string LoginID
    {
        get { return _loginid; }
        set { SetProperty(ref _loginid, value); }
    }

    private string _guidid;
    public string GuidID
    {
        get { return _guidid; }
        set { SetProperty(ref _guidid, value); }
    }

    private UserTypes _usertype;
    public UserTypes UserType
    {
        get { return _usertype; }
        set { SetProperty(ref _usertype, value); }
    }

    private bool _acceptnews;
    public bool AcceptNews
    {
        get { return _acceptnews; }
        set { SetProperty(ref _acceptnews, value); }
    }

    private string _url;
    public string Url
    {
        get { return _url; }
        set { SetProperty(ref _url, value); }
    }
    public List<string> Addresses { get; set; }
    public List<string> Privileges { get; set; }
    public List<string> Groups { get; set; }
    public List<string> Licenses { get; set; }

    private string _firstname;
    public string FirstName
    {
        get { return _firstname; }
        set { SetProperty(ref _firstname, value); }
    }

    private string _lastname;
    public string LastName
    {
        get { return _lastname; }
        set { SetProperty(ref _lastname, value); }
    }

    private string _email;
    public string Email
    {
        get { return _email; }
        set { SetProperty(ref _email, value); }
    }

    private string _password;
    public string Password
    {
        get { return _password; }
        set { SetProperty(ref _password, value); }
    }

    private string _emailconfirmed;
    public string EmailConfirmed
    {
        get { return _emailconfirmed; }
        set { SetProperty(ref _emailconfirmed, value); }
    }

    private string _passwordconfirmed;
    public string PasswordConfirmed
    {
        get { return _passwordconfirmed; }
        set { SetProperty(ref _passwordconfirmed, value); }
    }

    private string _address;
    public string Address
    {
        get { return _address; }
        set { SetProperty(ref _address, value); }
    }

    private string _city;
    public string City
    {
        get { return _city; }
        set { SetProperty(ref _city, value); }
    }

    private string _region;
    public string Region
    {
        get { return _region; }
        set { SetProperty(ref _region, value); }
    }

    private string _postalcode;
    public string PostalCode
    {
        get { return _postalcode; }
        set { SetProperty(ref _postalcode, value); }
    }

    private string _country;
    public string Country
    {
        get { return _country; }
        set { SetProperty(ref _country, value); }
    }

    private string _fax;
    public string Fax
    {
        get { return _fax; }
        set { SetProperty(ref _fax, value); }
    }

    private string _faxnumber;
    public string FaxNumber
    {
        get { return _faxnumber; }
        set { SetProperty(ref _faxnumber, value); }
    }

    private string _phonenumber;
    public string PhoneNumber
    {
        get { return _phonenumber; }
        set { SetProperty(ref _phonenumber, value); }
    }

    private string _phonenumberconfirmed;
    public string PhoneNumberConfirmed
    {
        get { return _phonenumberconfirmed; }
        set { SetProperty(ref _phonenumberconfirmed, value); }
    }

    private string _company;
    public string Company
    {
        get { return _company; }
        set { SetProperty(ref _company, value); }
    }

    private string _companynumber;
    public string CompanyNumber
    {
        get { return _companynumber; }
        set { SetProperty(ref _companynumber, value); }
    }

    private string _department;
    public string Department
    {
        get { return _department; }
        set { SetProperty(ref _department, value); }
    }

    private string _position;
    public string Position
    {
        get { return _position; }
        set { SetProperty(ref _position, value); }
    }

    private string _profile;
    public string Profile
    {
        get { return _profile; }
        set { SetProperty(ref _profile, value); }
    }

    private bool _isloggedin;
    public bool IsLoggedin
    {
        get { return _isloggedin; }
        set { SetProperty(ref _isloggedin, value); }
    }

    private bool _isadmin;
    public bool IsAdmin
    {
        get { return _isadmin; }
        set { SetProperty(ref _isadmin, value); }
    }

}
}
