using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using UE4Intellisense.Actions;
using UE4Intellisense.Model;
using UE4Intellisense.Processor;

namespace UE4Intellisense.Providers
{
    internal sealed class UE4SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly UE4SuggestedActionsSourceProvider _factory;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;
        private readonly UE4Processor _ue4Processor;

        public UE4SuggestedActionsSource(UE4SuggestedActionsSourceProvider ue4SuggestedActionsSourceProvider,
            ITextView textView, ITextBuffer textBuffer)
        {
            _factory = ue4SuggestedActionsSourceProvider;
            _textBuffer = textBuffer;
            _textView = textView;
            _ue4Processor = new UE4Processor(_factory.NavigatorService.GetTextStructureNavigator(textBuffer));
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                TextExtent extent;
                if (!TryGetWordUnderCaret(out extent))
                    return false;
                // don't display the action if the extent has whitespace
                if (!extent.IsSignificant)
                    return false;
                UE4MacroStatement ue4Statement;
                if (!_ue4Processor.TryGetUE4Macro(range.Start, out ue4Statement))
                    return false;
                return true;
            }, cancellationToken);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            TextExtent extent;
            UE4MacroStatement ue4Statement;
            if (TryGetWordUnderCaret(out extent) && extent.IsSignificant &&
                _ue4Processor.TryGetUE4Macro(extent.Span.End, out ue4Statement))
            {
                bool inMeta;
                _ue4Processor.ParseSpecifiers(extent.Span.End, ref ue4Statement, out inMeta);

                UE4Specifier[] datasource;
                switch (ue4Statement.MacroConst)
                {
                    case UE4Macros.UCLASS:
                        datasource = new[] {UE4SpecifiersSource.UC}.SelectMany(a => a).ToArray();
                        break;
                    case UE4Macros.UFUNCTION:
                        datasource = new[] {UE4SpecifiersSource.UF}.SelectMany(a => a).ToArray();
                        break;
                    case UE4Macros.UPROPERTY:
                        datasource = new[] {UE4SpecifiersSource.UP}.SelectMany(a => a).ToArray();
                        break;
                    case UE4Macros.UINTERFACE:
                        datasource = new[] {UE4SpecifiersSource.UI}.SelectMany(a => a).ToArray();
                        break;
                    case UE4Macros.USTRUCT:
                        datasource = new[] {UE4SpecifiersSource.US}.SelectMany(a => a).ToArray();
                        break;
                    default:
                        datasource = new UE4Specifier[0];
                        break;
                }
                var specifiersNotInList =
                    ue4Statement.Specifiers
                    .Select(c=>c.Split(new []{'='},StringSplitOptions.RemoveEmptyEntries)[0].ToUpper())//only take specifier and not its params
                    .Where(c => !datasource.Select(s => s.Name.ToUpper()).Contains(c)); //toupper compare
                var moreThanOneSpecInGroup = datasource.Where(s => ue4Statement.Specifiers.Contains(s.Name))
                    .Where(c => c.Group != null)
                    .GroupBy(s => s.Group)
                    .Any(g => g.Count() > 1);


                var removeInvalid = new SuggestedActionSet(
                    specifiersNotInList.Select(
                        s =>
                        {
                            var specStart = ue4Statement.SpecifiersSpan.Start +
                                            ue4Statement.SpecifiersSpan.GetText().IndexOf(s, StringComparison.Ordinal);
                            var specEnd = specStart + s.Length;
                            var ss = new SnapshotSpan(specStart, specEnd);
                            return new UE4SpecifierNotValidSuggestedAction(
                                _textView.TextSnapshot.CreateTrackingSpan(ss.Span,
                                    SpanTrackingMode.EdgeInclusive), ue4Statement);
                        }).ToArray(),
                    SuggestedActionSetPriority.None, ue4Statement.SpecifiersSpan);
                
                return new[] {removeInvalid};
            }
            return Enumerable.Empty<SuggestedActionSet>();
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            var caret = _textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            var navigator = _factory.NavigatorService.GetTextStructureNavigator(_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }
    }

    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("UE4 Specifiers Suggested Actions")]
    [ContentType("C/C++")]
    internal class UE4SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null && textView == null)
            {
                return null;
            }
            return new UE4SuggestedActionsSource(this, textView, textBuffer);
        }
    }
}