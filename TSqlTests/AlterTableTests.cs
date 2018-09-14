using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TSql;

namespace TSqlTests
{
    [TestClass]
    public class AlterTableTests
    {
        [TestMethod]
        public void TestAddConstraint()
        {
            var sql = "ALTER TABLE [HumanResources].[Employee] ADD  CONSTRAINT [DF_Employee_SalariedFlag]  DEFAULT ((1)) FOR [SalariedFlag]";

            TestUtilities.TestParsing(sql, parser => parser.alter_table());
        }

        [TestMethod]
        public void TestAddOnDeleteConstraint()
        {
            var sql = @"ALTER TABLE [Sales].[SalesOrderDetail]  WITH CHECK ADD  CONSTRAINT [FK_SalesOrderDetail_SalesOrderHeader_SalesOrderID] FOREIGN KEY([SalesOrderID])
REFERENCES[Sales].[SalesOrderHeader]([SalesOrderID])
ON DELETE CASCADE";

            TestUtilities.TestParsing(sql, parser => parser.alter_table());
        }

        [TestMethod]
        public void TestAddCheckConstraint()
        {
            var sql = "ALTER TABLE [HumanResources].[Employee]  WITH CHECK ADD  CONSTRAINT [CK_Employee_BirthDate] CHECK  (([BirthDate]>='1930-01-01' AND [BirthDate]<=dateadd(year,(-18),getdate())))";

            TestUtilities.TestParsing(sql, parser => parser.alter_table());
        }
    }
}
