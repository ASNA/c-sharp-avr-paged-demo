using System;
using System.Collections.Generic;
using System.Web.Mvc;
using ASNA.IBMiAccess;
using mvc_with_avr.Models;

namespace mvc_with_avr.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult ClearSearch()
        {
            Session.Remove("filter");
            return RedirectToAction("Index", new { page = 1 });
        }

        public ActionResult Index(int? page)
        {
            int pageNumber = (page.HasValue) ? page.Value : 1;

            PagedDataManager pdm = new PagedDataManager();
            if (Session["filter"] != null) 
            {
                pdm.WhereClause = getWhereClause(Session["filter"].ToString());
            }

            List<CustomerPageModel> cpm = pdm.GetPageData(PageNumber: pageNumber);

            ViewBag.MorePages = pdm.MorePagesToShow;
            ViewBag.NextPage = pageNumber + 1;
            ViewBag.PrevPage = pageNumber - 1;
            return View(cpm);
        }

        private string getWhereClause(string filter)
        {
            return String.Format("WHERE LOWER(CONCAT(cmcustno,trim(cmname))) LIKE '%{0}%'", filter.ToLower().Trim());
        }

        [HttpPost]
        public ActionResult Search(string search)
        {
            Session["filter"] = search;
            return RedirectToAction("Index", new { page = 1 }); 
        }
    }
}