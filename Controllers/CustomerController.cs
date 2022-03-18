using System;
using System.Collections.Generic;
using System.Web.Mvc;
using ASNA.IBMiAccess;
using mvc_with_avr.Models;

namespace mvc_with_avr.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult Index(int? page)
        {
            CustomerPagedModelViewModel viewmodel = new CustomerPagedModelViewModel();

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

            viewmodel.Customers = cpm;

            return View(viewmodel);
        }

        private string getWhereClause(string filter)
        {
            return String.Format("WHERE LOWER(CONCAT(cmcustno,trim(cmname))) LIKE '%{0}%'", filter.ToLower().Trim());
        }

        [HttpPost]
        public ActionResult SetFilter(string filter)
        {
            Session["filter"] = filter;
            return RedirectToAction("Index", new { page = 1 }); 
        }

        public ActionResult ClearFilter()
        {
            Session.Remove("filter");
            return RedirectToAction("Index", new { page = 1 });
        }


    }
}