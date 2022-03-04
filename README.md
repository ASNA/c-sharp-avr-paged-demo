



![](https://asna.com/filebin/marketing/chrome_2JGFYu1tyz.png)


### Routes

```
using System.Web.Mvc;
using System.Web.Routing;

namespace mvc_with_avr
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Customer",
                url: "customers/{page}",
                defaults: new { controller = "Customer", action = "Index",
                     page = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index",
                      id = UrlParameter.Optional }
            );
        }
    }
}
```

### Controller

```
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
    }
}
```

### Repository

```
using System.Collections.Generic;
using ASNA.DataGateHelper;
using mvc_with_avr.Models;

namespace ASNA.IBMiAccess
{
    public class PagedDataManager
    {
        const int PAGE_SIZE = 12;

        public bool MorePagesToShow;

        List<CustomerPageModel> Customers = new List<CustomerPageModel>();

        public List<CustomerPageModel> GetPageData(int PageNumber)
        {
            ASNA.VisualRPG.Runtime.Database DGDB;

            DataGateDB DGDBManager = new DataGateDB("*Public/Leyland");
            DGDB = DGDBManager.GetConnectionForAVR();

            PagedData pd = new PagedData(DGDB: DGDB, 
                                         LibraryName:"qtemp", 
                                         ProgramLibrary: "rpzimmie", 
                                         RPGProgramToCall: "sqlimmed", 
                                         PageSize: PAGE_SIZE, 
                                         CustomClassType: typeof(CustomerPageModel));

            pd.AfterRowRead += OnAfterRowRead;

            // Add SELECT clause. 
            pd.AddSQLSelect("select cmcustno, cmname");
            // Add FROM clauses. 
            pd.AddSQLFrom("from examples/cmastnewL2");
            // Add ORDER BY clause.
            pd.AddSQLOrderBy("order by cmname, cmcustno");

            // Read a page.
            // This method first executes SQL on the IBM i with the rpzimmie/sqlimmed 
            // RPG program then reads the result file created in QTEMP to populate 
            // CustomerPageModel instances. 
            pd.WriteThenReadTempFile(PageNumber);

            MorePagesToShow = pd.MoreRecords;

            return this.Customers;
        }

        private void OnAfterRowRead(object sender, AfterRowReadArgs e)
        {
            CustomerPageModel cpm = (CustomerPageModel)e.CustomClassInstance;

            // Save CustomerPageModel instance for the row just read from Examples/CMastNewL2.
            Customers.Add(cpm);
        }
    }
}
```


### Page

```
@{
    Layout = "~/Views/Shared/_Layout.cshtml";
}

@model IEnumerable<mvc_with_avr.Models.CustomerPageModel>

@{
    ViewBag.Title = "Customer List";
    ViewBag.Message = "Populated with ANSA Visual RPG and DataGate";
    string disabledPreviousButton = (ViewBag.PrevPage <= 1) ? "disabled-anchor" : "";
    string disabledNextButton = (ViewBag.MorePages) ? "" : "disabled-anchor"; 
}

<h2>@ViewBag.Title</h2>
<h3>@ViewBag.Message</h3>

<div class="buttons-container">
    <div class="mr-3">
        Note that the Edit and Delete buttons aren't hooked up.
    </div>
    <div>
        <a href="@Url.Action("index", "customer", new { page = ViewBag.PrevPage })" 
           class="btn btn-primary btn-sm active mr-3 @disabledPreviousButton">Prev page</a>

        <a href="@Url.Action("index", "customer", new { page = ViewBag.NextPage })" 
           class="btn btn-primary btn-sm active @disabledNextButton">Next page</a>
    </div>
</div> 

<table class="table">
    <tr>
        <th>
            Number
        </th>
        <th>
            Name
        </th>
        <th>Action</th>
    </tr>

    @foreach (var item in Model)
    {
        <tr>
            <td>
                @Html.DisplayFor(modelItem => item.CMCustNo)
            </td>
            <td>
                @Html.DisplayFor(modelItem => item.CMName)
            </td>
            <td>
                @Html.ActionLink("Edit", "Edit", "Customer", new { id = item.CMCustNo }) |
                @Html.ActionLink("Delete", "Delete", "Customer", new { id = item.CMCustNo })
            </td>
        </tr>
    }
</table>
```
