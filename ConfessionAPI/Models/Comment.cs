using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;

namespace ConfessionAPI.Models
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime PostTime { get; set; }

        public int? Like { get; set; }

        public int? Dislike { get; set; }

        public int? Report { get; set; }

        public bool Active { get; set; }

        public bool IsEdited { get; set; }

        public int? LevelComment { get; set; }

        [ForeignKey("ChildComments")]
        public Guid? ParentId { get; set; }

        [ForeignKey("Replier")]
        public string AccountId { get; set; } // Tài khoản trả lời

        [ForeignKey("Post")]
        public Guid PostId { get; set; } // Mã bài viết

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // ======================================================
        // Not Mapped
        // ======================================================

        [NotMapped]
        public string NickName { get; set; }

        [NotMapped]
        public string Avatar { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================

        [JsonIgnore]
        public virtual Account Replier { get; set; }
        [JsonIgnore]
        public virtual Post Post { get; set; }
        [DataMember]
        public virtual IList<CommentLike> CommentLikes { get; set; }
        //[JsonIgnore]
        public virtual IList<Comment> ChildComments { get; set; }
    }

    public class CommentLike
    {
        [Key]
        [Column(Order = 1)]
        [ForeignKey("Comment")]
        public Guid Id { get; set; }

        [Key]
        [Column(Order = 2)]
        [MaxLength(500)]
        public string UserID { get; set; }

        public DateTime TimeLike { get; set; }

        public bool IsLiked { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================
        [JsonIgnore]
        public virtual Comment Comment { get; set; }
    }
}