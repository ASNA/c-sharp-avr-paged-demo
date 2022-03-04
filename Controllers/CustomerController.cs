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
            Session.Remove("whereClause");
            return RedirectToAction("Index", new { page = 1 });
        }

        public ActionResult Index(int? page)
        {
            int pageNumber = (page.HasValue) ? page.Value : 1;

            PagedDataManager pdm = new PagedDataManager();
            if (Session["whereClause"] != null) 
            {
                pdm.WhereClause = Session["whereClause"].ToString();
            }

            List<CustomerPageModel> cpm =
                   pdm.GetPageData(PageNumber: pageNumber);

            ViewBag.MorePages = pdm.MorePagesToShow;
            ViewBag.NextPage = pageNumber + 1;
            ViewBag.PrevPage = pageNumber - 1;
            return View(cpm);
        }

        [HttpPost]
        public ActionResult Search(string search)
        {
            string whereClause = String.Format("WHERE LOWER(CONCAT(cmcustno,trim(cmname))) LIKE '%{0}%'", search.ToLower().Trim());
            Session["whereClause"] = whereClause;

            return RedirectToAction("Index", new { page = 1 }); 
        }
    }
}