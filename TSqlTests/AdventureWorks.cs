using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TSql;

namespace TSqlTests
{
    [TestClass]
    public class AdventureWorks
    {
        class ErrorListener : IAntlrErrorListener<IToken>
        {
            public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                Assert.Fail($"{msg} around \"{offendingSymbol.Text}\" on line {line} position {charPositionInLine}");
            }
        }

        [TestMethod]
        public void TestAdventureWorksCreate()
        {
            var adventureWorksCreate = Scripts.AdventureWorksCreate();

            TSqlParser parser = TSqlUtilities.GetParser(adventureWorksCreate);
            parser.AddErrorListener(new ErrorListener());
            var command = parser.tsql_file();
            Assert.AreEqual(0, parser.NumberOfSyntaxErrors);
        }
    }
}
