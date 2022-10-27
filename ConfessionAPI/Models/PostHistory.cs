using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public enum PostHistoryAction
    {
        Create,
        Delete,
        UpdateFull
    }
    public class PostHistory
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Post")]
        public Guid PostId { get; set; }

        [ForeignKey("Account")]
        [StringLength(128)]
        public string AccountId { get; set; }
        public DateTime ActionTime { get; set; }

        public PostHistoryAction HistoryAction { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================

        [JsonIgnore]
        public virtual Account Account { get; set; }
        [JsonIgnore]
        public virtual Post Post { get; set; }
    }
}