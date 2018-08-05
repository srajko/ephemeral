using Antlr4.Runtime;

namespace TSql
{
    public static class TSqlUtilities
    {
        public static TSqlParser GetParser(string sql)
        {
            var stream = new AntlrInputStream(sql);
            var caseInsensitiveStream = new CaseChangingCharStream(stream, upper: true);
            TSqlLexer lexer = new TSqlLexer(caseInsensitiveStream);
            var tokenStream = new CommonTokenStream(lexer);
            return new TSqlParser(tokenStream);
        }
    }
}
