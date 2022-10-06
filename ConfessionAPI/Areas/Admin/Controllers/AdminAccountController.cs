using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ConfessionAPI.DAL;
using Microsoft.AspNet.Identity.Owin;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    public class AdminAccountController : AdminController
    {
        public AdminAccountController()
        {
            
        }

        private ConfessionDbContext db = new ConfessionDbContext();
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [HttpGet]
        public IHttpActionResult Index()
        {
            var accounts = db.IdentityUsers.ToList();
            return Json(accounts);
        }


        //[HttpPost]
        //public async Task<IHttpActionResult> Edit()
        //{

        //}
    }
}
