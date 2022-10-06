using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public class Picture
    {
        [Key]
        public Guid Id { get; set; }

        [StringLength(1000)]
        public string Path { get; set; }

        public bool Active { get; set; }

        [ForeignKey("Post")]
        public Guid PostId { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================
        [JsonIgnore]
        public virtual Post Post { get; set; }
    }
}