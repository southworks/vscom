using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ACOM.DocumentationSample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult GetStarted()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}