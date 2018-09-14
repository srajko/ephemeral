using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSqlTests
{
    class ErrorListener : IAntlrErrorListener<IToken>
    {
        public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Assert.Fail($"{msg} around \"{offendingSymbol.Text}\" on line {line} position {charPositionInLine}");
        }
    }
}
