using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserCommentController : UserController
    {

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
                var userId = subCmt.AccountId;
                var account = db.IdentityUsers.Find(userId);
                if (account != null)
                {
                    if (account.UserProfile.NickName != null)
                    {
                        subCmt.NickName = account.UserProfile.NickName;
                    }
                    else
                    {
                        subCmt.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                    }

                    subCmt.Avatar = account.UserProfile.Avatar;
                }
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
                var userId = cmt.AccountId;
                var account = db.IdentityUsers.Find(userId);
                if (account != null)
                {
                    if (account.UserProfile.NickName != null)
                    {
                        cmt.NickName = account.UserProfile.NickName;
                    }
                    else
                    {
                        cmt.NickName = "User@" + account.UserProfile.Id.Split('-')[0];
                    }

                    cmt.Avatar = account.UserProfile.Avatar;
                }
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

        [HttpPost]
        public IHttpActionResult Create()
        {
            try
            {
                var cmtRequest = HttpContext.Current.Request["comment"];
                var cmt = JsonConvert.DeserializeObject<Comment>(cmtRequest);

                cmt.Id = Guid.NewGuid();
                cmt.AccountId = User.Identity.GetUserId();
                cmt.PostTime = DateTime.Now;
                cmt.Active = true;
                db.Comments.Add(cmt);
                db.SaveChanges();
                return Json(cmt);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult Edit()
        {
            try
            {
                var cmt = HttpContext.Current.Request["Comment"];
                Comment newCmt = JsonConvert.DeserializeObject<Comment>(cmt);

                return Json(newCmt);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
