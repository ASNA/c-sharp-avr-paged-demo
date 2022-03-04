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
            int pageNumber = (page.HasValue) ? page.Value : 1;

            PagedDataManager pdm = new PagedDataManager();
            List<CustomerPageModel> cpm =
                   pdm.GetPageData(PageNumber: pageNumber);

            ViewBag.MorePages = pdm.MorePagesToShow;
            ViewBag.NextPage = ++pageNumber;
            ViewBag.PrevPage = pageNumber - 2;
            return View(cpm);
        }

        [HttpPost]
        public ActionResult Search(string search)
        {
            string formSearch = search;

            return View();
        }
    }
}