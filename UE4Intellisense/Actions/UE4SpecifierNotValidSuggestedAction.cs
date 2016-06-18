using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using UE4Intellisense.Model;

namespace UE4Intellisense.Actions
{
    internal class UE4SpecifierNotValidSuggestedAction : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly UE4MacroStatement _ue4Statement;
        private readonly ITextSnapshot _snapshot;

        public UE4SpecifierNotValidSuggestedAction(ITrackingSpan span, UE4MacroStatement ue4Statement)
        {
            _span = span;
            _ue4Statement = ue4Statement;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            DisplayText = $"'{span.GetText(_snapshot)}' is not valid {ue4Statement.MacroConst} specifier";
        }

        public string DisplayText { get; }

        public string IconAutomationText => null;

        ImageMoniker ISuggestedAction.IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasActionSets => true;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            var removeSuggestedAction = new UE4SpecifierRemoveSuggestedAction(_span);
            return Task.FromResult(new[] { new SuggestedActionSet(new ISuggestedAction[] { removeSuggestedAction }) }.AsEnumerable());
        }

        public bool HasPreview => false;

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public void Dispose()
        {
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            //Default -> remove
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _span.TextBuffer.Replace(_span.GetSpan(_snapshot), "");
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    internal class UE4SpecifierRemoveSuggestedAction : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly ITextSnapshot _snapshot;

        public UE4SpecifierRemoveSuggestedAction(ITrackingSpan span)
        {
            _span = span;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            DisplayText = $"Remove '{span.GetText(_snapshot)}' specifier";
        }

        public string DisplayText { get; }

        public string IconAutomationText => null;

        ImageMoniker ISuggestedAction.IconMoniker => default(ImageMoniker);

        public string InputGestureText => null;

        public bool HasActionSets => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public bool HasPreview => true;

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock { Padding = new Thickness(5) };
            textBlock.Inlines.Add(new Run { Text = "" });
            return Task.FromResult<object>(textBlock);
        }

        public void Dispose()
        {
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _span.TextBuffer.Replace(_span.GetSpan(_snapshot), "");
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}