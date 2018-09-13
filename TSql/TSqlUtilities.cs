using Antlr4.Runtime;
using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<(TSqlParser.BatchContext, string)> GetBatches(string sql)
        {
            var sqlFile = TSqlUtilities.GetParser(sql).tsql_file();

            foreach (var batch in sqlFile.batch())
            {
                var start = batch.Start.StartIndex;
                var goStatements = batch.go_statement();
                var end = goStatements.Any()
                    ? goStatements.First().Start.StartIndex - 1
                    : batch.Stop.StopIndex;

                if (end >= start)
                {
                    yield return (batch, sql.Substring(start, end - start + 1));
                }
            }
        }
    }
}
