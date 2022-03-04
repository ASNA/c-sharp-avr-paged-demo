

## How to integrate a C# ASP.NET MVC app with your IBM i _without_ Client Access


This repo provides a working C# MVC 5.x Web that uses an assembly created with ASNA Visual RPG (AVR) to present a paged list of customers. You can also set a filter and page through the filtered result set. 

>This project _does not_ use any part of the IBM i Client Access or Access Client (has there ever been dumber naming!). It works entirely with a DLL created with ASNA Visual RPG and the Visual RPG/DataGate runtime. 

The paged data is presented in a page as shown below:

![](https://asna.com/filebin/marketing/chrome_pcMuoGNHv7.png)

The primary enabler for this project is the ASNA.DataGateHelper.dll which is [freely available at the ASNA.DataGateHelper GitHub repository.](https://github.com/ASNA/ASNA.DataGateHelper)

This project is written 100# in C#. Its use of ASNA Visual RPG is provided through the ASNA.DataGateHelper.dll. You _do not_ need to know ASNA Visual RPG to use this DLL. 

In a nutshell, using a class in the AVR DLL, the MVC app calls an ILE RPG program which issues an SQL state that queries an IBM i table and writes the results of that query to a temporary table in QTEMP. This SQL query uses the IBM i's SQL LIMIT/OFFSET features to limit the result set to a "page" of data. The page is requested by page number. Given the page size and the page number, the LIMIT and OFFSET is calculated internally. Programmatically, all MVC cares about is what page should be displayed--the grunge of getting the correct result set is buried away in the AVR DLL.

Immediately after writing those results, another class in the DLL then reads the results and returns them to the MVC controller as a `List<T>` class. C# doesn't know, nor does it care, where or how that `List<T>` was created and populated. 

The figure below provides a high-level look at this logic:

![](https://asna.com/filebin/marketing/mspaint_rkElbxoppS.png)


### The example's use case

The use case presented here is needing to allow a user to quickly and easily selecting a customer with which to do something. The customer file is available on the IBM i with this schema:

```
Database Name.: *Public/Leyland
Library.......: examples
File..........: CMastNewL2
Format........: RCMMastL2
Key field(s)..: cmname, cmcustno
Type..........: simple logical
Base file.....: Examples/CMastNew
Description...: CustomerByName
Record length.: 151

Field name        Data type                    Description
------------------------------------------------------------------
CMName            Type(*Char) Len(40)          Customer Name
CMCustNo          Type(*Packed) Len(9,0)       Customer Number

CMAddr1           Type(*Char) Len(35)          Address Line 1
CMCity            Type(*Char) Len(30)          City
CMState           Type(*Char) Len(2)           State
CMCntry           Type(*Char) Len(2)           Country Code
CMPostCode        Type(*Char) Len(10)          Postal Code (zip)
CMActive          Type(*Char) Len(1)           Active = 1, else 0
CMFax             Type(*Packed) Len(10,0)      Fax Number
CMPhone           Type(*Char) Len(20)          Main Phone
------------------------------------------------------------------
```

This file is indexed on name and customer number. In this case, the app needs only to use the CMName and CMCustNo fields. The SQL used for this will be similar the SQL shown below: 

```
CREATE TABLE qtemp/AYPYVOEBNN as 
(
    WITH result AS ( 
        SELECT  cmcustno, cmname 
        FROM  examples/cmastnewL2 
        ORDER BY  cmname, cmcustno 
        LIMIT 13 
        OFFSET 24 
    ) SELECT * FROM result 
) WITH DATA
```

If we could take a peek at that result set, it would something like this:

![](https://asna.com/filebin/marketing/tn5250_vxGsQP3YHd.png)

> A quick note for the eagle-eyed. If you read the project's code (which is also presented below) you'll notice that the page size in this demo app is 12 (via the `PAGE_SIZE` constant)--yet 13 rows are returned in the result set. Why is that? The AVR logic fetches one more than requested and if the size of the result set returned is (page size + 1) then the `MorePagesToShow` property is true, otherwise it is false (look for its use near the end of the controller). This property provides the ability to disable the `next` button if there aren't any more rows to show. The AVR logic presents only the rows requested (the rows in the grid) and ignores the extra row. 

When one of those result set rows is read, the model below is presents a populated `CustomerPageModel` instance to the app for reach row of the result set. When all of the rows of the result set have been read, a populated `List <CustomerPageModel>` is available to the controller. 

```
using System.ComponentModel.DataAnnotations;

namespace mvc_with_avr.Models
{
    public class CustomerPageModel
    {
        [DisplayFormat(DataFormatString = "{0:00000}", ApplyFormatInEditMode = true)]
        public System.Decimal CMCustNo { get; set; }
        public string CMName { get; set; }
    }
}
```

### Controller

```
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
```

### Repository

This is the page-specific code you need to be able to fetch data by page. Having created this class, in the controller all you need to do to get the list needed to populate the grid is: 

```
List<CustomerPageModel> cpm = pdm.GetPageData(PageNumber: PageNumber);
```

In this case, `cpm` is passed to the page as its data model.

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

        public string WhereClause { get; set; } = "";

        public List<CustomerPageModel> GetPageData(int PageNumber)
        {
            ASNA.VisualRPG.Runtime.Database DGDB;

            DataGateDB DGDBManager = new DataGateDB("*Public/Leyland");
            DGDB = DGDBManager.GetConnectionForAVR();

            PagedData pd = new PagedData(DGDB: DGDB, 
                                         LibraryName: "qtemp", 
                                         ProgramLibrary: "rpzimmie", 
                                         RPGProgramToCall: "sqlimmed", 
                                         PageSize: PAGE_SIZE, 
                                         CustomClassType: typeof(CustomerPageModel));

            pd.AfterRowRead += OnAfterRowRead;

            // Add SELECT clause. 
            pd.AddSQLSelect("select cmcustno, cmname");
            // Add FROM clauses. 
            pd.AddSQLFrom("from examples/cmastnewL2");

            // Add WHERE clause if provided. 
            if (!string.IsNullOrEmpty(this.WhereClause)) {
                pd.AddSQLWhere(this.WhereClause);
            } 

            // Add ORDER BY clause.
            pd.AddSQLOrderBy("order by cmname, cmcustno");

            // Read a page.
            // This method first executes SQL on the IBM i with the rpzimmie/sqlimmed 
            // RPG program then reads the result file created in QTEMP to populate 
            // CustomerPageModel instances. 
            pd.WriteThenReadTempFile(PageNumber);

            MorePagesToShow = pd.MoreRecords;

            DGDBManager.Disconnect();

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
    ViewBag.Message = "Written using C# referencing a couple of ASNA Visual RPG and DataGate DLLs";
    string disabledPreviousButton = (ViewBag.NextPage == 2) ? "disabled-anchor" : "";
    string disabledNextButton = (ViewBag.MorePages) ? "" : "disabled-anchor";
    string disabledFilterButton = (Session["filter"] == null) ? "disabled-anchor" : "";
    string filter = (Session["filter"] != null) ? Session["filter"].ToString() : "";
}

<h2>@ViewBag.Title</h2>
<h3>@ViewBag.Message</h3>

<div class="right-justified-container">

    @using (Html.BeginForm("SetFilter", "Customer", FormMethod.Post, 
                new { @class = "mr-1" }))
    {
        <input type="Text" name="filter" id="filter" value="@filter" />
        <button class="btn btn-primary btn-sm" type="submit">Filter</button>
    }

    <div>
        <a href="@Url.Action("clearFilter", "customer")"
           class="btn btn-primary btn-sm mr-3 @disabledFilterButton">Clear filter</a>
    </div>

    <div>
        <a href="@Url.Action("index", "customer", new { page = 1 })"
           class="btn btn-primary btn-sm mr-1 @disabledPreviousButton">First page</a>

        <a href="@Url.Action("index", "customer", new { page = ViewBag.PrevPage })"
           class="btn btn-primary btn-sm mr-1 @disabledPreviousButton">Prev page</a>

        <a href="@Url.Action("index", "customer", new { page = ViewBag.NextPage })"
           class="btn btn-primary btn-sm @disabledNextButton">Next page</a>
    </div>
</div>

<table class="table zebra-stripe">
    <tr>
        <th class="col-number">
            Number
        </th>
        <th class="col-name">
            Name
        </th>
        <th class="col-action">Action</th>
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

<div class="right-justified-container">
    Note that the Edit and Delete buttons aren't hooked up.
</div>
```


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
