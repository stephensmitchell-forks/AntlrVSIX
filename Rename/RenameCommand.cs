﻿using AntlrVSIX.Package;

namespace AntlrVSIX.Rename
{
    using Antlr4.Runtime;
    using AntlrVSIX.Extensions;
    using AntlrVSIX.Grammar;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Linq;
    using System;

    internal sealed class RenameCommand
    {
        public const int _command_id = 0x0102;
        public static readonly Guid _command_set = new Guid("0c1acc31-15ac-417c-86b2-eefdc669e8bf");
        private readonly Package _package;
        private MenuCommand _menu_item;

        private RenameCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }
            _package = package;
            OleMenuCommandService command_service = this.ServiceProvider.GetService(
                typeof(IMenuCommandService)) as OleMenuCommandService;
            if (command_service != null)
            {
                CommandID menu_command_id = new CommandID(_command_set, _command_id);
                _menu_item = new MenuCommand(RenameCallback, menu_command_id);
                _menu_item.Enabled = false;
                _menu_item.Visible = false;
                command_service.AddCommand(_menu_item);
            }
        }

        public bool Enabled
        {
            set { _menu_item.Enabled = value; }
        }

        public bool Visible
        {
            set { _menu_item.Visible = value; }
        }

        public static RenameCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return this._package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new RenameCommand(package);
        }

        private void RenameCallback(object sender, EventArgs e)
        {
            // Highlight the symbol, reposition it to the beginning of it.
            // Every character changes all occurrences of the symbol.

            string classification = AntlrLanguagePackage.Instance.Classification;
            SnapshotSpan span = AntlrLanguagePackage.Instance.Span;
            ITextView view = AntlrLanguagePackage.Instance.View;

            // First, find out what this view is, and what the file is.
            ITextBuffer buffer = view.TextBuffer;
            ITextDocument doc = buffer.GetTextDocument();
            string path = doc.FilePath;
            IVsTextView vstv = IVsTextViewExtensions.GetIVsTextView(path);

            List<IToken> where = new List<IToken>();
            List<ParserDetails> where_details = new List<ParserDetails>();
            foreach (var kvp in ParserDetails._per_file_parser_details)
            {
                string file_name = kvp.Key;
                ParserDetails details = kvp.Value;
                if (classification == AntlrVSIX.Constants.ClassificationNameNonterminal)
                {
                    var it = details._ant_nonterminals.Where(
                        (t) => t.Text == span.GetText());
                    where.AddRange(it);
                    foreach (var i in it) where_details.Add(details);
                }
                else if (classification == AntlrVSIX.Constants.ClassificationNameTerminal)
                {
                    var it = details._ant_terminals.Where(
                        (t) => t.Text == span.GetText());
                    where.AddRange(it);
                    foreach (var i in it) where_details.Add(details);
                }
                else if (classification == AntlrVSIX.Constants.ClassificationNameLiteral)
                {
                    var it = details._ant_literals.Where(
                        (t) => t.Text == span.GetText());
                    where.AddRange(it);
                    foreach (var i in it) where_details.Add(details);
                }
            }
            if (!where.Any()) return;

            IWpfTextView wpftv = vstv.GetIWpfTextView();
            if (wpftv == null) return;

            // Create new span in the appropriate view.
            ITextSnapshot cc = wpftv.TextBuffer.CurrentSnapshot;
            SnapshotSpan ss = new SnapshotSpan(cc, span.Start.Position, 1);
            SnapshotPoint sp = ss.Start;

            // Enable highlighter.
            RenameHighlightTagger.Enabled = true;

            // Put cursor on symbol.
            wpftv.Caret.MoveTo(sp);     // This sets cursor, bot does not center.
            AntlrVSIX.Package.Menus.ResetMenus();
        }
    }
}
