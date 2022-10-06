using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public class Category
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Alias { get; set; } // Tên gọi khác

        public string Description { get; set; }

        public bool Active { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================
        [JsonIgnore]
        public IList<Post> Posts { get; set; }
    }
}