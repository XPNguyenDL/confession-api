using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ConfessionAPI.Controllers
{
    public class ImageController : Controller
    {
        // GET: Image
        public ActionResult Post(string id)
        {
            var dir = Server.MapPath("~/Uploads/Pictures/Post/");
            var path = Path.Combine(dir, id); //validate the path for security or use other means to generate the path.
            return base.File(path, "image/jpeg");
        }
    }
}