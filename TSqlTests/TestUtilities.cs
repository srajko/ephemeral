using Antlr4.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TSql;

namespace TSqlTests
{
    static class TestUtilities
    {
        static public void TestParsing(string sql, Func<TSqlParser, ParserRuleContext> parse)
        {
            TSqlParser parser = TSqlUtilities.GetParser(sql);
            parser.AddErrorListener(new ErrorListener());
            var parsed = parse(parser);
            Assert.AreEqual(0, parser.NumberOfSyntaxErrors);
            Assert.AreEqual(sql.Length - 1, parsed.Stop.StopIndex);
        }
    }
}
