using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSql;

namespace Ephemeral
{
    class MemoryOptimizedVisitor : TSqlParserBaseVisitor<string>
    {
        List<Replacement> Replacements = new List<Replacement>();

        private void Remove(IParseTree parseTree, string replacement = "")
        {
            if (parseTree is ParserRuleContext context)
            {
                Replacements.Add(new Replacement { SourceIndex = context.Start.StartIndex, SourceLength = context.Stop.StopIndex - context.Start.StartIndex + 1, Target = replacement });
            }
            else if (parseTree is ITerminalNode terminalNode)
            {
                Replacements.Add(new Replacement { SourceIndex = terminalNode.Symbol.StartIndex, SourceLength = terminalNode.Symbol.StopIndex - terminalNode.Symbol.StartIndex + 1, Target = replacement });
            }

        }

        private void RemoveOnAndNextChild(ParserRuleContext context)
        {
            var removeNextChild = false;
            foreach (var child in context.children)
            {
                if (removeNextChild)
                {
                    removeNextChild = false;
                    Remove(child);
                }
                if (child is ITerminalNode && string.Compare(child.GetText(), "ON", ignoreCase: true) == 0)
                {
                    removeNextChild = true;
                    Remove(child);
                }
            }
        }

        public override string VisitCreate_table([NotNull] TSqlParser.Create_tableContext context)
        {
            RemoveOnAndNextChild(context);
            Replacements.Add(new Replacement { SourceIndex = context.Stop.StopIndex + 1, Target = " WITH (MEMORY_OPTIMIZED=ON, DURABILITY = SCHEMA_ONLY) " });
            return base.VisitCreate_table(context);
        }

        public override string VisitColumn_definition([NotNull] TSqlParser.Column_definitionContext context)
        {
            foreach (var child in context.children)
            {
                if (child is ITerminalNode && child.GetText() == "SPARSE")
                {
                    Remove(child);
                }
            }
            return base.VisitColumn_definition(context);
        }

        public override string VisitData_type([NotNull] TSqlParser.Data_typeContext context)
        {
            var dataTypeId = context.GetChild(0).GetText().ToUpper();

            if (dataTypeId == "TIMESTAMP" || dataTypeId == "[TIMESTAMP]")
            {
                Remove(context.GetChild(0), "[BINARY](8)");
            }
            if (dataTypeId == "NTEXT" || dataTypeId == "[NTEXT]")
            {
                Remove(context.GetChild(0), "[NVARCHAR](MAX)");
            }
            if (context.ChildCount >= 4 && context.children[1].GetText() == "IDENTITY")
            {
                var value = int.Parse(context.children[3].GetText());
                if (value != 1)
                {
                    Remove(context.children[3], "1");
                }
            }
            return base.VisitData_type(context);
        }

        public override string VisitColumn_def_table_constraints([NotNull] TSqlParser.Column_def_table_constraintsContext context)
        {
            var hasPrimaryKeyOrIndex = false;
            string firstColumn = null;
            foreach (var child in context.children)
            {
                if (child is TSqlParser.Column_def_table_constraintContext)
                {
                    if (firstColumn == null && (child.GetChild(0) is TSqlParser.Column_definitionContext columnDefinitionChild))
                    {
                        IParseTree descendant = child;
                        do
                        {
                            descendant = descendant.GetChild(0);
                        } while (!(descendant is ITerminalNode));
                        firstColumn = descendant.GetText();
                    }
                    if (child.GetChild(0) is TSqlParser.Table_constraintContext constraintChild)
                    {
                        if (
                            string.Compare(constraintChild.GetChild(2).GetText(), "PRIMARY", ignoreCase: true) == 0 ||
                            string.Compare(constraintChild.GetChild(0).GetText(), "PRIMARY", ignoreCase: true) == 0
                        )
                        {
                            hasPrimaryKeyOrIndex = true;
                        }
                    }
                }
            }
            if (!hasPrimaryKeyOrIndex)
            {
                Replacements.Add(new Replacement { SourceIndex = context.Stop.StopIndex + 1, SourceLength = 0, Target = $", INDEX IX_INDEX NONCLUSTERED ({firstColumn}) " });
            }
            return base.VisitColumn_def_table_constraints(context);
        }

        public override string VisitTable_constraint([NotNull] TSqlParser.Table_constraintContext context)
        {
            RemoveOnAndNextChild(context);
            foreach (var child in context.children)
            {
                if (child is TSqlParser.ClusteredContext clusteredContextChild)
                {
                    Replacements.Add(new Replacement { SourceIndex = clusteredContextChild.Start.StartIndex, SourceLength = clusteredContextChild.Stop.StopIndex - clusteredContextChild.Start.StartIndex + 1, Target = "NONCLUSTERED" });
                }
            }
            return base.VisitTable_constraint(context);
        }


        public override string VisitIndex_options([NotNull] TSqlParser.Index_optionsContext context)
        {
            var targetOptions = new TSqlParser.Index_optionsContext(null, context.invokingState);
            targetOptions.children = new List<IParseTree>();
            var removeNextChild = false;
            var indexOptionsCount = 0;
            foreach (var child in context.children)
            {
                var removed = false;
                if (child is TSqlParser.Index_optionContext)
                {
                    var firstGrandchildText = child.GetChild(0).GetText().ToUpper();
                    if (new[] { "PAD_INDEX", "IGNORE_DUP_KEY", "STATISTICS_NORECOMPUTE", "ALLOW_ROW_LOCKS", "ALLOW_PAGE_LOCKS" }.Contains(firstGrandchildText))
                    {
                        removed = true;
                        if (targetOptions.children.Last().GetText() == ",")
                        {
                            targetOptions.RemoveLastChild();
                        }
                        else
                        {
                            removeNextChild = true;
                        }
                    }
                    else
                    {
                        indexOptionsCount++;
                    }
                }
                if (!removed)
                {
                    if (removeNextChild)
                    {
                        removeNextChild = false;
                    }
                    else
                    {
                        targetOptions.children.Add(child);
                    }
                }
            }
            var test = indexOptionsCount != 0 ? targetOptions.GetText() : "";
            Replacements.Add(new Replacement { SourceIndex = context.Start.StartIndex, SourceLength = context.Stop.StopIndex - context.Start.StartIndex + 1, Target = test });
            return base.VisitIndex_options(context);
        }

        public string Transform(string command, int offset)
        {
            var transformedCommand = new StringBuilder(command);

            foreach (var replacement in Replacements.OrderByDescending(r => r.SourceIndex))
            {
                transformedCommand.Remove(replacement.SourceIndex - offset, replacement.SourceLength);
                transformedCommand.Insert(replacement.SourceIndex - offset, replacement.Target);
            }

            return transformedCommand.ToString();
        }

        struct Replacement
        {
            public int SourceIndex;
            public int SourceLength;
            public string Target;
        }
    }
}
