using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using ConfessionAPI.Models;

namespace ConfessionAPI.Areas.User.Data
{
    public class PostCreateViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }

        public string Content { get; set; }

        
        public DateTime? CreatedTime { get; set; }

        public int? Like { get; set; }
        public int? Dislike { get; set; }
        public int? Report { get; set; }
        public bool PrivateMode { get; set; }
        //public PostStatus Status { get; set; }

        public bool Active { get; set; }

        [StringLength(500)]
        [DataType(DataType.MultilineText)]
        public HttpPostedFileBase Upload { get; set; }

        public Guid[] SelectedCategories { get; set; }
        public virtual IList<Picture> Pictures { get; set; }
    }
}