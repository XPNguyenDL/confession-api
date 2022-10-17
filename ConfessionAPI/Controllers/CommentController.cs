using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;

namespace ConfessionAPI.Controllers
{
    public class CommentController : ApiController
    {
        private ConfessionDbContext db = new ConfessionDbContext();

        /// <summary>
        /// Phương thức tìm nhóm còn của comment
        /// từ danh sách tất cả các nhóm allCmt
        /// </summary>
        /// <param name="comment">Comment cha tìm comment con</param>
        /// <param name="allCmts">Danh sách tất cả mặt hàng</param>
        private void AddSubComment(Comment comment, List<Comment> allCmts)
        {
            comment.ChildComments = allCmts
                .Where(x => x.ParentId == comment.Id)
                .ToList();
            foreach (var subCmt in comment.ChildComments)
            {
                AddSubComment(subCmt, allCmts);
            }
        }

        private List<Comment> PolulateComment(Guid postId)
        {
            var allCmts = db.Comments.Where(x => x.PostId == postId).ToList();

            var groupCmts = allCmts
                .Where(x => !x.ParentId.HasValue || x.ParentId == null)
                .ToList();
            foreach (var cmt in groupCmts)
            {
                AddSubComment(cmt, allCmts);
            }

            return groupCmts;
        }

        [HttpPost]
        public IHttpActionResult Index()
        {
            var postId = Guid.Parse(HttpContext.Current.Request["Id"]);
            var cmts = PolulateComment(postId);
            return Json(cmts);
        }
    }
}
