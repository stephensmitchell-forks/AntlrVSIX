﻿namespace LanguageServer
{
    using Graphs;
    using System.Collections.Generic;
    using System.Linq;
    using Workspaces;
    using Symtab;
    using System;

    public class Module
    {

        public static int GetIndex(int line, int column, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return 0;
            int index = 0;
            var buffer = pd.Code;
            if (buffer == null) return 0;
            int cur_line = 0;
            int cur_col = 0;
            for (; ;)
            {
                if (cur_line > line) break;
                if (cur_line >= line && cur_col >= column) break;
                var ch = buffer[index];
                if (ch == '\r')
                {
                    if (buffer[index + 1] == '\n')
                    {
                        cur_line++;
                        cur_col = 0;
                        index += 2;
                    }
                }
                else if (ch == '\n')
                {
                    cur_line++;
                    cur_col = 0;
                    index += 1;
                }
                else
                {
                    cur_col += 1;
                    index += 1;
                }
                if (index >= buffer.Length) break;
            }
            return index;
        }

        public static (int, int) GetLineColumn(int index, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return (0, 0);
            int cur_index = 0;
            var buffer = pd.Code;
            if (buffer == null) return (0, 0);
            int cur_line = 0; // zero based LSP.
            int cur_col = 0; // zero based LSP.
            for (; ; )
            {
                if (cur_index >= buffer.Length) break;
                if (cur_index >= index) break;
                var ch = buffer[cur_index];
                if (ch == '\r')
                {
                    if (buffer[cur_index + 1] == '\n')
                    {
                        cur_line++;
                        cur_col = 0;
                        cur_index += 2;
                    }
                }
                else if (ch == '\n')
                {
                    cur_line++;
                    cur_col = 0;
                    cur_index += 1;
                }
                else
                {
                    cur_col += 1;
                    cur_index += 1;
                }
                if (cur_index >= buffer.Length) break;
            }
            return (cur_line, cur_col);
        }

        public static QuickInfo GetQuickInfo(int index, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return null;
            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            var gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null) return null;
            Antlr4.Runtime.Tree.IParseTree p = pt;
            pd.Attributes.TryGetValue(p, out IList<CombinedScopeSymbol> list_value);
            var q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            var range = new Range(new Index(q.Symbol.StartIndex), new Index(q.Symbol.StopIndex + 1));
            var found = pd.Tags.TryGetValue(q, out int tag_type);
            if (!found) return null;
            if (list_value == null || list_value.Count == 0)
            {
                return new QuickInfo() { Display = gd.Map[tag_type], Range = range };
            }
            if (list_value.Count == 1)
            {
                var value = list_value.First();
                var name = value as Symtab.ISymbol;
                string show = name?.Name;
                if (value is Symtab.Literal)
                {
                    show = ((Symtab.Literal)value).Cleaned;
                }
                if (gd.PopUpDefinition[tag_type] != null)
                {
                    var fun = gd.PopUpDefinition[tag_type];
                    var mess = fun(pd, p);
                    if (mess != null)
                    {
                        return new QuickInfo() { Display = mess, Range = range };
                    }
                }
                var display = gd.Map[tag_type]
                    + "\n"
                    + show;
                return new QuickInfo() { Display = display, Range = range };
            }
            {
                var display = "Ambiguous -- ";
                foreach (var value in list_value)
                {
                    var name = value as Symtab.ISymbol;
                    string show = name?.Name;
                    if (value is Symtab.Literal)
                    {
                        show = ((Symtab.Literal)value).Cleaned;
                    }
                    if (gd.PopUpDefinition[tag_type] != null)
                    {
                        var fun = gd.PopUpDefinition[tag_type];
                        var mess = fun(pd, p);
                        if (mess != null)
                        {
                            display = display + mess;
                        }
                    }
                    else
                    {
                        display = display + gd.Map[tag_type]
                            + "\n"
                            + show;
                    }
                }
                return new QuickInfo() { Display = display, Range = range };
            }
        }

        public static int GetTag(int index, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return -1;
            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            var gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null) return -1;
            Antlr4.Runtime.Tree.IParseTree p = pt;
            var q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            var found = pd.Tags.TryGetValue(q, out int tag_type);
            if (found) return tag_type;
            if (q.Symbol == null) return -1;
            var found2 = pd.Comments.TryGetValue(q.Symbol, out int tag2);
            if (found2) return tag2;
            return -1;
        }

        public static DocumentSymbol GetDocumentSymbol(int index, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return null;
            Antlr4.Runtime.Tree.IParseTree pt = LanguageServer.Util.Find(index, doc);
            var gd = GrammarDescriptionFactory.Create(doc.FullPath);
            if (pt == null) return default(DocumentSymbol);
            Antlr4.Runtime.Tree.IParseTree p = pt;
            var q = p as Antlr4.Runtime.Tree.TerminalNodeImpl;
            var found = pd.Tags.TryGetValue(q, out int tag_type);
            if (!found) return null;
            if (q.Symbol == null) return null;
            return new DocumentSymbol()
            {
                name = q.Symbol.Text,
                range = new Range(q.Symbol.StartIndex, q.Symbol.StopIndex),
                kind = tag_type
            };
        }

        public static IEnumerable<DocumentSymbol> Get(Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return new List<DocumentSymbol>();
            var combined = new List<DocumentSymbol>();
            foreach (var p in pd.Tags)
            {
                if (p.Key.Symbol == null) continue;
                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Symbol.Text,
                        range = new Range(p.Key.Symbol.StartIndex, p.Key.Symbol.StopIndex),
                        kind = p.Value
                    });
            }
            foreach (var p in pd.Comments)
            {
                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Text,
                        range = new Range(p.Key.StartIndex, p.Key.StopIndex),
                        kind = p.Value
                    });
            }

            // Sort the list.
            var sorted_combined_tokens = combined.OrderBy(t => t.range.Start.Value).ThenBy(t => t.range.End.Value);
            return sorted_combined_tokens;
        }

        public static IEnumerable<DocumentSymbol> Get(Range range, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return new List<DocumentSymbol>();
            var combined = new System.Collections.Generic.List<DocumentSymbol>();
            foreach (var p in pd.Tags)
            {
                if (p.Key.Symbol == null) continue;
                int start_token_start = p.Key.Symbol.StartIndex;
                int end_token_end = p.Key.Symbol.StopIndex + 1;
                if (start_token_start > range.End.Value) continue;
                if (end_token_end < range.Start.Value) continue;
                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Symbol.Text,
                        range = new Range(p.Key.Symbol.StartIndex, p.Key.Symbol.StopIndex),
                        kind = p.Value
                    });
            }
            foreach (var p in pd.Comments)
            {
                int start_token_start = p.Key.StartIndex;
                int end_token_end = p.Key.StopIndex + 1;
                if (start_token_start > range.End.Value) continue;
                if (end_token_end < range.Start.Value) continue;
                combined.Add(
                    new DocumentSymbol()
                    {
                        name = p.Key.Text,
                        range = new Range(p.Key.StartIndex, p.Key.StopIndex),
                        kind = p.Value
                    });
            }

            // Sort the list.
            IEnumerable<DocumentSymbol> result;
            var sorted_combined_tokens = combined.OrderBy(t => t.range.Start.Value).ThenBy(t => t.range.End.Value);
            result = sorted_combined_tokens;
            return result;
        }

        public static IEnumerable<Range> GetErrors(Range range, Document doc)
        {
            var pd = ParserDetailsFactory.Create(doc);
            if (pd.ParseTree == null) return new List<Range>();
            var result = new List<Range>();
            foreach (var p in pd.Errors)
            {
                var q = p as Antlr4.Runtime.Tree.ErrorNodeImpl;
                if (q == null) continue;
                if (q.Payload == null) continue;
                var y = q.Payload.StartIndex;
                var z = q.Payload.StopIndex;
                if (y < 0)
                    y = 0;
                if (z < 0)
                    z = 0;
                var a = y;
                var b = z + 1;
                int start_token_start = a;
                int end_token_end = b;
                if (start_token_start > range.End.Value) continue;
                if (end_token_end < range.Start.Value) continue;
                var r = new Range(new Index(a), new Index(b));
                result.Add(r);
            }
            return result;
        }

        public static IList<Location> FindDef(int index, Document doc)
        {
            var result = new List<Location>();
            if (doc == null) return result;
            var ref_pt = Util.Find(index, doc);
            if (ref_pt == null) return result;
            var ref_pd = ParserDetailsFactory.Create(doc);
            ref_pd.Attributes.TryGetValue(ref_pt, out IList<Symtab.CombinedScopeSymbol> list_values);
            foreach (var value in list_values)
            {
                if (value == null) continue;
                var @ref = value as Symtab.ISymbol;
                if (@ref == null) continue;
                var def = @ref.resolve();
                if (def == null) continue;
                var def_file = def.file;
                if (def_file == null) continue;
                var def_item = Workspaces.Workspace.Instance.FindDocument(def_file);
                if (def_item == null) continue;
                var new_loc = new Location()
                {
                    Range = new Range(def.Token.StartIndex, def.Token.StopIndex),
                    Uri = def_item
                };
                result.Add(new_loc);
            }
            return result;
        }

        public static IEnumerable<Location> FindRefsAndDefs(int index, Document doc)
        {
            var result = new List<Location>();
            var ref_pt = Util.Find(index, doc);
            if (ref_pt == null) return result;
            var ref_pd = ParserDetailsFactory.Create(doc);
            ref_pd.Attributes.TryGetValue(ref_pt, out IList<Symtab.CombinedScopeSymbol> list_value);
            foreach (var value in list_value)
            {
                if (value == null) continue;
                var @ref = value as Symtab.ISymbol;
                if (@ref == null) continue;
                if (@ref.Token == null) continue;
                var def = @ref.resolve();
                if (def == null) continue;
                if (def.Token == null) continue;
                List<Antlr4.Runtime.Tree.TerminalNodeImpl> where = new List<Antlr4.Runtime.Tree.TerminalNodeImpl>();
                var refs = ref_pd.Refs.Where(
                    (t) =>
                    {
                        Antlr4.Runtime.Tree.TerminalNodeImpl x = t.Key;
                        if (x == @ref.Token) return true;

                        ref_pd.Attributes.TryGetValue(x, out IList<Symtab.CombinedScopeSymbol> list_v);
                        foreach (var v in list_v)
                        {
                            var vv = v as Symtab.ISymbol;
                            if (vv == null) return false;
                            if (vv.resolve() == def) return true;
                            return false;
                        }
                        return false;
                    }).Select(t => t.Key);
                if (def != null)
                {
                    if (def.file == @ref.file)
                        result.Add(
                           new Location()
                           {
                               Range = new Range(def.Token.StartIndex, def.Token.StopIndex),
                               Uri = Workspaces.Workspace.Instance.FindDocument(def.file)
                           });
                }
                foreach (var r in refs)
                {
                    result.Add(
                            new Location()
                            {
                                Range = new Range(r.Symbol.StartIndex, r.Symbol.StopIndex),
                                Uri = Workspaces.Workspace.Instance.FindDocument(r.Symbol.InputStream.SourceName)
                            });
                }
            }
            return result;
        }

        private static Digraph<ParserDetails> ConstructGraph(IEnumerable<ParserDetails> to_do)
        {
            Digraph<ParserDetails> g = new Digraph<ParserDetails>();
            HashSet<ParserDetails> done = new HashSet<ParserDetails>();
            Stack<ParserDetails> stack = new Stack<ParserDetails>();
            foreach (var f in to_do)
            {
                stack.Push(f);
            }

            while (stack.Count > 0)
            {
                var f = stack.Pop();
                g.AddVertex(f);
                done.Add(f);
                foreach (var d in f.PropagateChangesTo)
                {
                    var d_doc = Workspace.Instance.FindDocument(d);
                    var d_pd = ParserDetailsFactory.Create(d_doc);
                    if (done.Contains(d_pd)) continue;
                    stack.Push(d_pd);
                }
            }

            foreach (var v in g.Vertices)
            {
                var deps = v.PropagateChangesTo;
                var doc = Workspace.Instance.FindDocument(v.FullFileName);
                var pd = ParserDetailsFactory.Create(doc);
                foreach (var d in deps)
                {
                    var d_doc = Workspace.Instance.FindDocument(d);
                    var d_pd = ParserDetailsFactory.Create(d_doc);
                    g.AddEdge(new DirectedEdge<ParserDetails>(pd, d_pd));
                }
            }

            return g;
        }

        public static List<ParserDetails> Compile()
        {
            try
            {
                var ws = Workspaces.Workspace.Instance;

                // Get all changed files.
                HashSet<ParserDetails> to_do = new HashSet<ParserDetails>();

            DoAgain:

                Workspaces.Loader.LoadAsync().Wait();
                foreach (var document in Workspaces.DFSContainer.DFS(ws))
                {
                    string file_name = document.FullPath;
                    if (file_name == null) continue;
                    var gd = LanguageServer.GrammarDescriptionFactory.Create(file_name);
                    if (gd == null) continue;
                    // file_name can be a URI, so this doesn't make sense.
                    //if (!System.IO.File.Exists(file_name)) continue;
                    var pd = ParserDetailsFactory.Create(document);
                    if (!pd.Changed) continue;
                    to_do.Add(pd);
                }
                Digraph<ParserDetails> g = ConstructGraph(to_do);
                foreach (var v in g.Vertices)
                {
                    v.Item.Changed = true; // Force.
                    v.Parse();
                }
                var changed = true;
                for (int pass = 0; changed; pass++)
                {
                    changed = false;
                    foreach (var v in g.Vertices)
                    {
                        int number_of_passes = v.Passes.Count;
                        if (pass < number_of_passes)
                        {
                            var reset = v.Pass(pass);
                            if (reset)
                            {
                                goto DoAgain;
                            }
                            changed = true;
                        }
                    }
                }
                foreach (var v in g.Vertices)
                {
                    v.GatherDefs();
                }
                foreach (var v in g.Vertices)
                {
                    v.GatherRefs();
                }
                foreach (var v in g.Vertices)
                {
                    v.GatherErrors();
                }
                return g.Vertices.ToList();
            }
            catch (Exception e)
            {
                Logger.Log.Notify(e.ToString());
            }
            return new List<ParserDetails>();
        }
    }
}
