using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using ConfessionAPI.Areas.Admin.Controllers;
using ConfessionAPI.Areas.User.Data;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;

namespace ConfessionAPI.Controllers
{
    public class CategoryController : ApiController
    {
        ConfessionDbContext db = new ConfessionDbContext();

        [HttpGet]
        public IHttpActionResult Index()
        {
            var result = db.Categories.ToList();
            return Json(result);
        }
    }
}
