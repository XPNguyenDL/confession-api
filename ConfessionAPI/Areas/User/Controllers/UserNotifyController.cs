using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ConfessionAPI.Models;
using Microsoft.AspNet.Identity;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserNotifyController : UserController
    {
        private List<Notification> Notifies()
        {
            var userId = User.Identity.GetUserId();
            var notifies = db.Notification.Where(x => x.UserID == userId).ToList();

            var dateDelete = DateTime.Now.AddDays(-14);
            var oldNotifies = notifies.Where(x => x.NotifyDate < dateDelete && x.IsRead == true).ToList();
            foreach (var notify in oldNotifies)
            {
                db.Notification.Remove(notify);
            }

            db.SaveChanges();

            notifies = db.Notification.Where(x => x.UserID == userId).ToList();
            notifies = notifies.OrderByDescending(s => s.NotifyDate).ToList();

            return notifies;
        }

        [HttpGet]
        public IHttpActionResult Index()
        {
            try
            {
                var notifies = Notifies();
                return Json(notifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet]
        public IHttpActionResult ReadAll()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var notifies = db.Notification.Where(x => x.UserID == userId && x.IsRead == false).ToList();
                foreach (var notification in notifies)
                {
                    notification.IsRead = true;
                    db.Entry(notification).State = EntityState.Modified;
                }

                db.SaveChanges();

                notifies = db.Notification.Where(x => x.UserID == userId)
                    .OrderByDescending(s => s.NotifyDate).ToList();
                return Json(notifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpGet]
        public IHttpActionResult DeleteAll()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var notifies = db.Notification.Where(x => x.UserID == userId && x.IsRead == true).ToList();
                foreach (var notification in notifies)
                {
                    db.Notification.Remove(notification);
                }

                db.SaveChanges();
                notifies = db.Notification.Where(x => x.UserID == userId).ToList();
                return Json(notifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult ReadNotify()
        {
            try
            {
                var idNotify = Guid.Parse(HttpContext.Current.Request["id"]);
                var userId = User.Identity.GetUserId();
                var notify = db.Notification.FirstOrDefault(s => s.Id == idNotify
                                                                    && s.UserID == userId);
                if (notify == null)
                {
                    ModelState.AddModelError("Error", "Thông báo không tồn tại");
                    return BadRequest(ModelState);
                }

                notify.IsRead = true;
                db.Entry(notify).State = EntityState.Modified;
                db.SaveChanges();

                var notifies = db.Notification.Where(x => x.UserID == userId)
                    .OrderByDescending(s => s.NotifyDate).ToList();
                return Json(notifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        public IHttpActionResult DeleteNotify()
        {
            try
            {
                var idNotify = Guid.Parse(HttpContext.Current.Request["id"]);
                var userId = User.Identity.GetUserId();
                var notify = db.Notification.FirstOrDefault(s => s.Id == idNotify
                                                                 && s.UserID == userId);
                if (notify == null)
                {
                    ModelState.AddModelError("Error", "Thông báo không tồn tại");
                    return BadRequest(ModelState);
                }

                db.Notification.Remove(notify);
                db.SaveChanges();

                var newNotifies = Notifies();
                return Json(newNotifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
