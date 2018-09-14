using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TSql;

namespace TSqlTests
{
    [TestClass]
    public class ColumnDefinitionTests
    {
        [TestMethod]
        public void TestXmlColumnDefinition()
        {
            var sql = "[Resume] [xml](CONTENT [HumanResources].[HRResumeSchemaCollection]) NULL";

            TestUtilities.TestParsing(sql, parser => parser.column_definition());
        }

        [TestMethod]
        public void TestExpressionColumnDefinition()
        {
            var sql = "[TotalDue]  AS (isnull(([SubTotal]+[TaxAmt])+[Freight],(0))) PERSISTED NOT NULL";

            TestUtilities.TestParsing(sql, parser => parser.column_definition());
        }

        [TestMethod]
        public void TestMaskedWithColumnDefinition()
        {
            var sql = "Email varchar(100) MASKED WITH (FUNCTION = 'email()') NULL";

            TestUtilities.TestParsing(sql, parser => parser.column_definition());
        }
    }
}
