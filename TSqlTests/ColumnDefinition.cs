using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TSql;

namespace TSqlTests
{
    [TestClass]
    public class ColumnDefinition
    {
        [TestMethod]
        public void TestXmlColumnDefinition()
        {
            var xmlColumnDefinition = "[Resume] [xml](CONTENT [HumanResources].[HRResumeSchemaCollection]) NULL";

            TSqlParser parser = TSqlUtilities.GetParser(xmlColumnDefinition);
            var columnDefinition = parser.column_definition();
            Assert.AreEqual("[Resume][xml](CONTENT[HumanResources].[HRResumeSchemaCollection])NULL", columnDefinition.GetText());
        }
    }
}
