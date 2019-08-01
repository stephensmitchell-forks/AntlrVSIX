﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace AntlrVSIX.Grammar
{
    internal class ParserDetails
    {
        public static Dictionary<string, ParserDetails> _per_file_parser_details =
            new Dictionary<string, ParserDetails>();

        public string full_file_name;
        public string Text;

        // Parser and parse tree.
        public ANTLRv4Parser _ant_parser = null;
        public IParseTree _ant_tree = null;
        public IParseTree[] _all_nodes = null;

        // List of all nonterminals and terminals in the given grammar.
        public IList<string> _ant_nonterminals_names;
        public IList<string> _ant_terminals_names;

        // List of all comments, terminal, nonterminal, and keyword tokens in the grammar.
        public IList<IToken> _ant_terminals;
        public IList<IToken> _ant_terminals_defining;
        public IList<IToken> _ant_nonterminals;
        public IList<IToken> _ant_nonterminals_defining;
        public IList<IToken> _ant_comments;
        public IList<IToken> _ant_keywords;
        public IList<IToken> _ant_literals;

        public void Parse(string plain_old_input_grammar, string ffn)
        {
            Text = plain_old_input_grammar;
            full_file_name = ffn;

            // Set up Antlr to parse input grammar.
            byte[] byteArray = Encoding.UTF8.GetBytes(plain_old_input_grammar);
            CommonTokenStream cts = new CommonTokenStream(
                new ANTLRv4Lexer(
                    new AntlrInputStream(
                        new StreamReader(
                            new MemoryStream(byteArray)).ReadToEnd())));
            _ant_parser = new ANTLRv4Parser(cts);

            // Set up another token stream containing comments. This might be
            // problematic as the parser influences the lexer.
            CommonTokenStream cts_off_channel = new CommonTokenStream(
                new ANTLRv4Lexer(
                    new AntlrInputStream(
                        new StreamReader(
                            new MemoryStream(byteArray)).ReadToEnd())),
                ANTLRv4Lexer.OFF_CHANNEL);

            // Get all comments.
            _ant_comments = new List<IToken>();
            while (cts_off_channel.LA(1) != ANTLRv4Parser.Eof)
            {
                IToken token = cts_off_channel.LT(1);
                if (token.Type == ANTLRv4Parser.BLOCK_COMMENT
                    || token.Type == ANTLRv4Parser.LINE_COMMENT)
                {
                    _ant_comments.Add(token);
                }
                cts_off_channel.Consume();
            }

            try
            {
                _ant_tree = _ant_parser.grammarSpec();
            }
            catch (Exception e)
            {
                // Parsing error.
            }

            _all_nodes = DFSVisitor.DFS(_ant_tree as ParserRuleContext).ToArray();

            //StringBuilder sb = new StringBuilder();
            //ParenthesizedAST(sb, "", _ant_tree, cts);
            //System.IO.File.WriteAllText("c:\\temp\\kkk.txt", sb.ToString());

            {
                // Get all nonterminal names from the grammar.
                IEnumerable<IParseTree> nonterm_nodes_iterator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl nonterm = n as TerminalNodeImpl;
                    if (n.Parent as ANTLRv4Parser.RulerefContext != null &&
                        nonterm?.Symbol.Type == ANTLRv4Parser.RULE_REF)
                        return true;
                    if (n.Parent as ANTLRv4Parser.ActionBlockContext != null)
                        return false;
                    if (n.Parent as ANTLRv4Parser.ParserRuleSpecContext != null &&
                        nonterm?.Symbol.Type == ANTLRv4Parser.RULE_REF)
                        return true;
                    return false;
                });
                _ant_nonterminals_names = nonterm_nodes_iterator.Select<IParseTree, string>(
                    (t) => (t as TerminalNodeImpl).Symbol.Text).ToArray();
            }

            {
                // Get all terminal names from the grammar.
                IEnumerable<IParseTree> term_nodes_iterator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl nonterm = n as TerminalNodeImpl;
                    return nonterm?.Symbol.Type == ANTLRv4Parser.TOKEN_REF;
                });
                _ant_terminals_names = term_nodes_iterator.Select<IParseTree, string>(
                    (t) => (t as TerminalNodeImpl).Symbol.Text).ToArray();
            }

            {
                // Get all defining and applied occurences of nonterminal names in grammar.
                IEnumerable<IParseTree> nonterm_nodes_iterator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl nonterm = n as TerminalNodeImpl;
                    if (n.Parent as ANTLRv4Parser.RulerefContext != null &&
                        nonterm?.Symbol.Type == ANTLRv4Parser.RULE_REF)
                        return true;
                    if (n.Parent as ANTLRv4Parser.ActionBlockContext != null)
                        return false;
                    if (n.Parent as ANTLRv4Parser.ParserRuleSpecContext != null &&
                        nonterm?.Symbol.Type == ANTLRv4Parser.RULE_REF)
                        return true;
                    return false;
                });
                _ant_nonterminals = nonterm_nodes_iterator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
                // Get all defining and applied occurences of nonterminal names in grammar.
                var iterator = nonterm_nodes_iterator.Where((IParseTree n) =>
                {
                    TerminalNodeImpl term = n as TerminalNodeImpl;
                    if (term == null) return false;
                    IRuleNode parent = term.Parent;
                    for (int i = 0; i < parent.ChildCount; ++i)
                    {
                        if (parent.GetChild(i) == term &&
                            i + 1 < parent.ChildCount &&
                            parent.GetChild(i + 1).GetText() == ":")
                            return true;
                    }
                    return false;
                });
                _ant_nonterminals_defining = iterator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
            }

            {
                // Get all defining and applied occurences of nonterminal names in grammar.
                IEnumerable<IParseTree> term_nodes_iterator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl term = n as TerminalNodeImpl;
                    if (term == null) return false;
                    if (!_ant_terminals_names.Contains(term.GetText())) return false;
                    // The token must be part of parserRuleSpec context.
                    for (var p = term.Parent; p != null; p = p.Parent)
                    {
                        if (p is ANTLRv4Parser.ParserRuleSpecContext ||
                            p is ANTLRv4Parser.LexerRuleSpecContext) return true;
                    }
                    return false;
                });
                _ant_terminals = term_nodes_iterator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
                // Get all defining nonterminal names in grammar.
                var iterator = term_nodes_iterator.Where((IParseTree n) =>
                {
                    TerminalNodeImpl term = n as TerminalNodeImpl;
                    if (term == null) return false;
                    IRuleNode parent = term.Parent;
                    for (int i = 0; i < parent.ChildCount; ++i)
                    {
                        if (parent.GetChild(i) == term &&
                            i + 1 < parent.ChildCount &&
                            parent.GetChild(i + 1).GetText() == ":")
                            return true;
                    }
                    return false;
                });
                _ant_terminals_defining = iterator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
            }

            {
                // Get all keyword tokens in grammar.
                IEnumerable<IParseTree> keywords_interator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl nonterm = n as TerminalNodeImpl;
                    if (nonterm == null) return false;
                    for (var p = nonterm.Parent; p != null; p = p.Parent)
                    {
                        // "parser grammar" "lexer grammar" etc.
                        if (p is ANTLRv4Parser.GrammarTypeContext) return true;
                        if (p is ANTLRv4Parser.OptionsSpecContext) return true;
                        // "options ..."
                        if (p is ANTLRv4Parser.OptionContext) return false;
                        // "import ..."
                        if (p is ANTLRv4Parser.DelegateGrammarsContext) return true;
                        if (p is ANTLRv4Parser.DelegateGrammarContext) return false;
                        // "tokens ..."
                        if (p is ANTLRv4Parser.TokensSpecContext) return true;
                        if (p is ANTLRv4Parser.IdListContext) return false;
                        // "channels ..."
                        if (p is ANTLRv4Parser.ChannelsSpecContext) return true;
                        if (p is ANTLRv4Parser.ModeSpecContext) return true;
                    }
                    return false;
                });
                _ant_keywords = keywords_interator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
            }

            {
                // Get all defining and applied occurences of nonterminal names in grammar.
                IEnumerable<IParseTree> lit_nodes_iterator = _all_nodes.Where((IParseTree n) =>
                {
                    TerminalNodeImpl term = n as TerminalNodeImpl;
                    if (term == null) return false;
                    // Chicken/egg problem. Assume that literals are marked
                    // with the appropriate token type.
                    if (term.Symbol == null) return false;
                    if (!(term.Symbol.Type == ANTLRv4Parser.STRING_LITERAL
                        || term.Symbol.Type == ANTLRv4Parser.INT
                        || term.Symbol.Type == ANTLRv4Parser.LEXER_CHAR_SET))
                        return false;

                    // The token must be part of parserRuleSpec context.
                    for (var p = term.Parent; p != null; p = p.Parent)
                    {
                        if (p is ANTLRv4Parser.ParserRuleSpecContext ||
                            p is ANTLRv4Parser.LexerRuleSpecContext) return true;
                    }
                    return false;
                });
                _ant_literals = lit_nodes_iterator.Select<IParseTree, IToken>(
                    (t) => (t as TerminalNodeImpl).Symbol).ToArray();
            }

            //pp.ErrorHandler = new MyErrorStrategy();
        }

        private int changed = 0;
        private bool first_time = true;

        private void StartLine(StringBuilder sb, string file_name, IParseTree tree, CommonTokenStream stream, int level = 0)
        {
            if (changed - level >= 0)
            {
                if (!first_time)
                {
                    for (int j = 0; j < level; ++j) sb.Append("  ");
                    for (int k = 0; k < 1 + changed - level; ++k) sb.Append(") ");
                    sb.AppendLine();
                }
                changed = 0;
                first_time = false;
            }
            changed = level;
            for (int j = 0; j < level; ++j) sb.Append("  ");
        }
        public void ParenthesizedAST(StringBuilder sb, string file_name, IParseTree tree, CommonTokenStream stream, int level = 0)
        {
            // Antlr always names a non-terminal with first letter lowercase,
            // but renames it when creating the type in C#. So, remove the prefix,
            // lowercase the first letter, and remove the trailing "Context" part of
            // the name. Saves big time on output!
            if (tree as TerminalNodeImpl != null)
            {
                TerminalNodeImpl tok = tree as TerminalNodeImpl;
                Interval interval = tok.SourceInterval;
                var inter = stream.GetHiddenTokensToLeft(tok.Symbol.TokenIndex);
                if (inter != null)
                    foreach (var t in inter)
                    {
                        StartLine(sb, file_name, tree, stream, level);
                        sb.AppendLine("( HIDDEN text=" + provide_escapes(t.Text));
                    }
                StartLine(sb, file_name, tree, stream, level);
                sb.AppendLine("( TOKEN i=" + tree.SourceInterval.a
                    + " t=" + provide_escapes(tree.GetText()));
            }
            else
            {
                var fixed_name = tree.GetType().ToString()
                    .Replace("Antlr4.Runtime.Tree.", "");
                fixed_name = Regex.Replace(fixed_name, "^.*[+]", "");
                fixed_name = fixed_name.Substring(0, fixed_name.Length - "Context".Length);
                fixed_name = fixed_name[0].ToString().ToLower()
                             + fixed_name.Substring(1);
                StartLine(sb, file_name, tree, stream, level);
                sb.Append("( " + fixed_name);
                if (level == 0) sb.Append(" File=\""
                    + file_name
                    + "\"");
                sb.AppendLine();
            }
            for (int i = 0; i < tree.ChildCount; ++i)
            {
                var c = tree.GetChild(i);
                ParenthesizedAST(sb, file_name, c, stream, level + 1);
            }
            if (level == 0)
            {
                for (int k = 0; k < 1 + changed - level; ++k) sb.Append(") ");
                sb.AppendLine();
                changed = 0;
            }
        }
        private string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, new CodeGeneratorOptions { IndentString = "\t" });
                    var literal = writer.ToString();
                    literal = literal.Replace(string.Format("\" +{0}\t\"", Environment.NewLine), "");
                    return literal;
                }
            }
        }
        public string provide_escapes(string s)
        {
            StringBuilder new_s = new StringBuilder();
            new_s.Append(ToLiteral(s));
            //for (var i = 0; i != s.Length; ++i)
            //{
            //    if (s[i] == '"' || s[i] == '\\')
            //    {
            //        new_s.Append('\\');
            //    }
            //    new_s.Append(s[i]);
            //}
            return new_s.ToString();
        }

    }
}
