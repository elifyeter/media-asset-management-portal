using Microsoft.AspNetCore.Identity;

namespace MediaPortal2.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}