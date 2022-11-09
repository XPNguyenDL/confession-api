using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace ConfessionAPI.Models
{
    // You can add profile data for the user by adding more properties to your Account class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class Account : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<Account> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
        


        [DataMember]
        public virtual UserProfile UserProfile { get; set; }

        [DataMember]
        public virtual IList<Comment> Comments { get; set; }

        [DataMember]
        public virtual IList<PostHistory> PostHistory { get; set; }

        [DataMember]
        public virtual IList<Notification> Notifications { get; set; }

        [NotMapped]
        public List<string> RoleTemps { get; set; }


        public string Otp { get; set; }

        public DateTime? OtpCreateDate { get; set; }

        public int? OtpWrongTime { get; set; }
    }
    
}