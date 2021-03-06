﻿
namespace AntlrVSIX.AggregateTagger
{
    using AntlrVSIX.Package;
    using AntlrVSIX.Tagger;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.ComponentModel.Composition;
    using Color = System.Drawing.Color;

    [Export(typeof(ITaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class AntlrClassifierProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IClassificationFormatMapService ClassificationFormatMapService = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagger<T> result = null;
            try
            {
                AntlrLanguagePackage package = AntlrLanguagePackage.Instance;
                VSColorTheme.ThemeChanged += UpdateTheme;
                result = buffer.Properties.GetOrCreateSingletonProperty(() => new AntlrClassifier(buffer)) as ITagger<T>;
                var classifier = result as AntlrClassifier;
                classifier.Initialize(aggregatorFactory,
                    ClassificationTypeRegistry, ClassificationFormatMapService);
            }
            catch (Exception exception)
            {
                Logger.Log.Notify(exception.StackTrace);
            }
            return result;
        }

        private void UpdateTheme(EventArgs e)
        {
            Color defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            Color defaultForeground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
            var formatMap = ClassificationFormatMapService.GetClassificationFormatMap(category: "code");
            try
            {
                formatMap.BeginBatchUpdate();
            }
            finally
            {
                formatMap.EndBatchUpdate();
            }
        }
    }
}
