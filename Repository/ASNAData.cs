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