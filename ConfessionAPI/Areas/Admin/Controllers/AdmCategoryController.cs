using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ConfessionAPI.DAL;
using ConfessionAPI.Models;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    public class AdmCategoryController : AdmController
    {

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
                    ModelState.AddModelError("Error", $"{category.Name} doesn't exist");
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

        [HttpPost]
        public IHttpActionResult Delete()
        {
            try
            {
                var cateId = Guid.Parse(HttpContext.Current.Request["id"]);
                var oldCategory = db.Categories.SingleOrDefault(s => s.Id == cateId);
                if (oldCategory != null)
                {
                    db.Categories.Remove(oldCategory);
                    db.SaveChanges();
                }
                var categories = db.Categories.ToList();
                return Json(categories);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
        }
    }
}
