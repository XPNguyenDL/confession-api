using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ConfessionAPI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class AdmController : ApiController
    {

    }
}
