using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSql;

namespace TSqlTests
{
    [TestClass]
    public class AdventureWorksTests
    {
        [TestMethod]
        public void TestAdventureWorksCreate()
        {
            TestUtilities.TestParsing(Scripts.AdventureWorksCreate(), parser => parser.tsql_file());
        }
    }
}
