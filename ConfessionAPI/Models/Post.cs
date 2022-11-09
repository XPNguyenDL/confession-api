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
    public enum PostStatus
    {
        Violate, // Vi phạm nội quy
        Valid // hợp lệ
    }

    public class Post
    {
        public Post()
        {

        }
        public Post(Post s)
        {
            this.Active = s.Active;
            this.Status = s.Status;
            this.Comments = s.Comments;
            this.CreatedTime = s.CreatedTime;
            this.Content = s.Content;
            this.Dislike = s.Dislike;
            this.Like = s.Like;
            this.Id = s.Id;
            this.Pictures = s.Pictures;
            this.PostLikes = s.PostLikes;
            this.PostReports = s.PostReports;
            this.Report = s.Report;
            
            this.PostHistories = s.PostHistories;
            this.Title = s.Title;
            this.PrivateMode = s.PrivateMode;
            this.RowVersion = s.RowVersion;
            if (s.Categories != null)
            {
                this.Categories = s.Categories.Select(c => new Category()
                {
                    Id = c.Id,
                    Name = c.Name,
                    Posts = c.Posts,
                    Active = c.Active,
                    Alias = c.Alias,
                    Description = c.Description,
                    RowVersion = c.RowVersion
                }).ToList();
            }

        }

        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(500)]
        public string Title { get; set; }

        // Nội dung mô tả bài viết
        [StringLength(5000)]
        public string Content { get; set; }

        [Required]
        public DateTime? CreatedTime { get; set; }

        public int? Like { get; set; }
        public int? Dislike { get; set; }
        public int? Report { get; set; }
        public PostStatus Status { get; set; }

        public bool PrivateMode { get; set; }

        // Đánh dấu xóa bài viết
        public bool Active { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================

        [NotMapped]
        public string NickName { get; set; }

        [NotMapped]
        public string Avatar { get; set; }

        [NotMapped]
        public int TotalCmt { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================
        [DataMember]
        public virtual IList<Category> Categories { get; set; }
        [DataMember]
        public virtual IList<Comment> Comments { get; set; }
        [DataMember]
        public virtual IList<PostHistory> PostHistories { get; set; }
        [DataMember]
        public virtual IList<Picture> Pictures { get; set; }
        [DataMember]
        public virtual IList<PostLike> PostLikes { get; set; }

        [DataMember]
        public virtual IList<PostReport> PostReports { get; set; }

    }

    public class PostLike
    {
        [Key]
        [Column(Order = 1)]
        [ForeignKey("Post")]
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
        public virtual Post Post { get; set; }
    }

    public class PostReport
    {
        [Key]
        [Column(Order = 1)]
        [ForeignKey("Post")]
        public Guid Id { get; set; }

        [Key]
        [Column(Order = 2)]
        [MaxLength(500)]
        public string UserID { get; set; }

        public DateTime TimeReport { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // ======================================================
        // Navigation properties
        // ======================================================

        [JsonIgnore]
        public virtual Post Post { get; set; }
    }


}