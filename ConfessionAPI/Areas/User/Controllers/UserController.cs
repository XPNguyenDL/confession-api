using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ConfessionAPI.DAL;

namespace ConfessionAPI.Areas.User.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : ApiController
    {
        public ConfessionDbContext db = new ConfessionDbContext();
    }
}
