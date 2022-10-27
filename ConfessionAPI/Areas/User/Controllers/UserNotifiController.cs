using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Identity;

namespace ConfessionAPI.Areas.User.Controllers
{
    public class UserNotifiController : UserController
    {
        [HttpGet]
        public IHttpActionResult Index()
        {
            try
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

                return Json(notifies);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
