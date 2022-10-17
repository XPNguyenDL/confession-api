using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using ConfessionAPI.Controllers;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public enum Gender
    {
        Male,
        Female,
        Other
    }
    public class UserProfile
    {
        [Key, ForeignKey("Account")]
        [StringLength(128)]
        public string Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(300)]
        public string LastName { get; set; }

        public string NickName { get; set; }
        public DateTime Birthday { get; set; }

        [StringLength(500)]
        public string Avatar { get; set; }

        public Gender Gender { get; set; }

        [StringLength(5000)]
        public string Description { get; set; }

        [StringLength(200)]
        public string Major { get; set; }

        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
        }

        // ======================================================
        // Navigation properties
        // ======================================================

        [JsonIgnore]
        public virtual Account Account { get; set; }
    }
}