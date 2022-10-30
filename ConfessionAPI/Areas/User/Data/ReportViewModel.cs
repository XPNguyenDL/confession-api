using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ConfessionAPI.Areas.User.Data
{
    public class ReportViewModel
    {
        public string Description { get; set; }

        public Guid PostId { get; set; }
    }
}