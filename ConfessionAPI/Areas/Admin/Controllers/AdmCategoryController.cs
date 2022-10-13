using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    public class AdmCategoryController : ApiController
    {
        private ConfessionDbContext db = new ConfessionDbContext();

        [HttpGet]
        public IHttpActionResult Index()
        {
            var categories = db.Categories.ToList();
            return Json(categories);
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
                    db.Categories.Add(category);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                return Json(e);
            }

            return Json(category);
        }

        [HttpPost]
        public IHttpActionResult Edit(Category category)
        {
            try
            {
                var oldCategory = db.Categories.SingleOrDefault(c => c.Id == category.Id);
                if (oldCategory != null)
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
                }
                else
                {
                    ModelState.AddModelError("Error", $"{category.Name} doesn  exist");
                    return BadRequest(ModelState);
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
