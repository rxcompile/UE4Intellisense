using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace UE4Intellisense
{
    internal class UE4SpecifiersCompletionSource : ICompletionSource
    {
        private UE4SpecifiersCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;

        public UE4SpecifiersCompletionSource(UE4SpecifiersCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var triggerPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot).GetValueOrDefault();

            var tracker = TrackUE4MacroSpecifier(triggerPoint);

            if (tracker == null)
                return;

            if (tracker.MacroConst == UE4Macros.UProperty)
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UProperty",
                  FindTokenSpanAtPosition(triggerPoint),
                  ConstructCompletions(UE4SpecifiersSource.UP, tracker.Specifiers),
                  null);

                completionSets.Add(ue4cs);
            }
            if (tracker.MacroConst == UE4Macros.UClass)
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UClass",
                  FindTokenSpanAtPosition(triggerPoint),
                  ConstructCompletions(UE4SpecifiersSource.UC, tracker.Specifiers),
                  null);

                completionSets.Add(ue4cs);
            }
            if (tracker.MacroConst == UE4Macros.UInterface)
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UInterface",
                  FindTokenSpanAtPosition(triggerPoint),
                  ConstructCompletions(UE4SpecifiersSource.UI, tracker.Specifiers),
                  null);

                completionSets.Add(ue4cs);
            }
            if (tracker.MacroConst == UE4Macros.UFunction)
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UFunction",
                  FindTokenSpanAtPosition(triggerPoint),
                  ConstructCompletions(UE4SpecifiersSource.UF, tracker.Specifiers),
                  null);

                completionSets.Add(ue4cs);
            }
            if (tracker.MacroConst == UE4Macros.UStruct)
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UStruct",
                  FindTokenSpanAtPosition(triggerPoint),
                  ConstructCompletions(UE4SpecifiersSource.US, tracker.Specifiers),
                  null);

                completionSets.Add(ue4cs);
            }
            session.SelectedCompletionSetChanged += SessionSelectedCompletionSetChanged;
        }

        private IEnumerable<Completion> ConstructCompletions(UE4Specifier[] compList, string[] specifiers)
        {
            var currentSpecs = compList.Where(g => specifiers.Contains(g.Name, StringComparer.InvariantCultureIgnoreCase));

            return compList
                .Where(g =>
                    g.GroupId == null || // all not correlated Specifiers
                    currentSpecs.All(t => t.GroupId != g.GroupId) || // and not specifier with same groupId
                    currentSpecs.Contains(g) // except the one allready written
                    )
                .Select(g => new Completion(g.Name, g.Name, g.Desc, null, null));
        }

        private UE4Statement TrackUE4MacroSpecifier(SnapshotPoint triggerPoint)
        {
            SnapshotPoint currentPoint = triggerPoint - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);

            SnapshotSpan statement = navigator.GetSpanOfEnclosing(extent.Span);
            var statementText = statement.GetText();

            var macros = typeof(UE4Macros).GetEnumNames().Aggregate("", (a, e) => a += "|" + e);

            var match = Regex.Match(statementText, $@"({macros})\((.*)\)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;
            if (!match.Groups[1].Success || !match.Groups[2].Success)
                return null;

            var macroConst = (UE4Macros)Enum.Parse(typeof(UE4Macros), match.Groups[1].Value);

            var contentPosition = statement.Start + match.Groups[2].Index;
            var contentEnd = contentPosition + match.Groups[2].Length;

            if (extent.Span.Start < contentPosition || extent.Span.End > contentEnd)
                return null;

            return new UE4Statement { MacroConst = macroConst, Specifiers = match.Groups[2].Value.Split(',').ToArray() };
        }

        private void SessionSelectedCompletionSetChanged(object sender, ValueChangedEventArgs<CompletionSet> e)
        {
            var session = sender as ICompletionSession;
            if (session == null)
                return;
            if (e.OldValue == null)
            {
                var ue4sc = session.CompletionSets.FirstOrDefault(set => set.Moniker == "ue4");
                if (ue4sc == null)
                    return;
                session.SelectedCompletionSet = ue4sc;
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(SnapshotPoint point)
        {
            SnapshotPoint currentPoint = point - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);

            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        private bool m_isDisposed;

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }

    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("C/C++")]
    [Name("UE4 completion")]
    internal class UE4SpecifiersCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new UE4SpecifiersCompletionSource(this, textBuffer);
        }
    }
}
