using Microsoft.AspNetCore.Identity;

namespace CampusOne.Infrastructure.Identity
{
     public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
