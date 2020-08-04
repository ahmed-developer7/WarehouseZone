using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;


namespace Ganedata.Core.Entities.Domain
{
    [Serializable]
    public class AuthUser : PersistableEntity<int>
    {
        public AuthUser()
        {
            AuthPermissions = new HashSet<AuthPermission>();
            AuthUserLogins = new HashSet<AuthUserLogin>();
            AuthUserprofiles = new HashSet<AuthUserprofile>();
        }

        [Key]
        [Display(Name = "User Id")]
        public int UserId { get; set; }
        [MaxLength(50)]
        [Remote("IsUserAvailable", "User", ErrorMessage = "User is  Already in Use. ")]
        [Required(ErrorMessage = "User Name is Required")]
        [Display(Name = "User Name")]
        public string UserName { get; set; }
        [Display(Name = "Password")]
        public string UserPassword { get; set; }
        [MaxLength(50)]
        [Display(Name = "First Name")]
        public string UserFirstName { get; set; }
        [MaxLength(50)]
        [Display(Name = "Last Name")]
        public string UserLastName { get; set; }
        [MaxLength(200)]
        [Display(Name = "Email")]
        public string UserEmail { get; set; }
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        [Display(Name = "SuperUser")]
        public Boolean? SuperUser { get; set; }
        public string UserCulture { get; set; }
        public string UserTimeZoneId { get; set; }
        public virtual ICollection<AuthPermission> AuthPermissions { get; set; }
        public virtual ICollection<AuthUserLogin> AuthUserLogins { get; set; }
        public virtual ICollection<AuthUserprofile> AuthUserprofiles { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        public string DisplayName
        {
            get { return UserLastName + ", " + UserFirstName; }
        }
    }
}
