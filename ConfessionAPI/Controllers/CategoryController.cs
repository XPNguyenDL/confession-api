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
    public class CategoryController : AdmController
    {
        ConfessionDbContext db = new ConfessionDbContext();

        [HttpGet]
        public IHttpActionResult Index()
        {
            var result = db.Categories.ToList();
            return Json(result);
        }

        [HttpPost]
        public IHttpActionResult Create(Category category)
        {
            try
            {
                if (db.Categories.SingleOrDefault(x => x.Alias == category.Alias) != null)
                {
                    ModelState.AddModelError("Error", $"{category.Alias} is exist");
                    return BadRequest(ModelState);
                }
                if (ModelState.IsValid)
                {
                    category.Id = Guid.NewGuid();
                    category.Active = true;
                    db.Categories.Add(category);
                    db.SaveChanges();
                }
                return Json(category);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }

        
    }
}
