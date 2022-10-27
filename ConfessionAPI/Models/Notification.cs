using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public enum TypeNotify
    {
        Like, 
        Comment,
        Report
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Account")]
        public string UserID { get; set; }

        public string NotifyUserId { get; set; }

        public string NotifyName { get; set; }

        public string Description { get; set; }

        public Guid PostId { get; set; }

        public DateTime NotifyDate { get; set; }

        public Boolean IsRead { get; set; }

        public TypeNotify TypeNotify { get; set; }

        [StringLength(500)]
        public string Avatar { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================
        [JsonIgnore]
        public virtual Account Account { get; set; }
    }
}