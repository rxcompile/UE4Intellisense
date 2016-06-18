using System;
using System.Collections.Generic;
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
    internal class UE4SpecifierCollisitionSuggestedAction : ISuggestedAction
    {
        private readonly ITrackingSpan _span;
        private readonly UE4MacroStatement _ue4Statement;
        private readonly ITextSnapshot _snapshot;
        private readonly string _lower;

        public UE4SpecifierCollisitionSuggestedAction(ITrackingSpan span, UE4MacroStatement ue4Statement)
        {
            _span = span;
            _ue4Statement = ue4Statement;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            _lower = span.GetText(_snapshot).ToLower();
            DisplayText = $"Convert '{span.GetText(_snapshot)}' to lower case";
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
            var textBlock = new TextBlock {Padding = new Thickness(5)};
            textBlock.Inlines.Add(new Run { Text = _lower });
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

            _span.TextBuffer.Replace(_span.GetSpan(_snapshot), _lower);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
