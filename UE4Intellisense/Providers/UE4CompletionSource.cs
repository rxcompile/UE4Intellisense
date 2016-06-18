using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using UE4Intellisense.Model;
using UE4Intellisense.Processor;

namespace UE4Intellisense.Providers
{
    internal class UE4SpecifiersCompletionSource : ICompletionSource
    {
        private readonly UE4SpecifiersCompletionSourceProvider _sourceProvider;
        private readonly ITextBuffer _textBuffer;
        private readonly UE4Processor _ue4Processor;

        private bool _isDisposed;

        public UE4SpecifiersCompletionSource(UE4SpecifiersCompletionSourceProvider sourceProvider,
            ITextBuffer textBuffer)
        {
            _sourceProvider = sourceProvider;
            _textBuffer = textBuffer;
            _ue4Processor = new UE4Processor(sourceProvider.NavigatorService.GetTextStructureNavigator(textBuffer));
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot).GetValueOrDefault();

            bool inMeta;
            UE4MacroStatement ue4Statement;

            if (!_ue4Processor.TryGetUE4Macro(triggerPoint, out ue4Statement))
                return;

            if (triggerPoint < ue4Statement.SpecifiersSpan.Start || triggerPoint > ue4Statement.SpecifiersSpan.End)
                return;

            _ue4Processor.ParseSpecifiers(triggerPoint, ref ue4Statement, out inMeta);

            SnapshotSpan wordSpan;
            var trackingSpan = FindTokenSpanAtPosition(triggerPoint, out wordSpan);
            switch (ue4Statement.MacroConst)
            {
                case UE4Macros.UPROPERTY:
                    if (inMeta)
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UPropertyMeta",
                            trackingSpan,
                            ConstructCompletions(
                                new[] {UE4SpecifiersSource.UMGeneric, UE4SpecifiersSource.UMP}.SelectMany(a => a)
                                    .ToArray(),
                                ue4Statement.MetaSpecifiers, wordSpan),
                            null));
                    else
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UProperty",
                            trackingSpan,
                            ConstructCompletions(UE4SpecifiersSource.UP, ue4Statement.Specifiers, wordSpan),
                            null));
                    break;
                case UE4Macros.UCLASS:
                    if (inMeta)
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UClassMeta",
                            trackingSpan,
                            ConstructCompletions(
                                new[] {UE4SpecifiersSource.UMGeneric, UE4SpecifiersSource.UMC}.SelectMany(a => a)
                                    .ToArray(),
                                ue4Statement.MetaSpecifiers, wordSpan),
                            null));
                    else
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UClass",
                            trackingSpan,
                            ConstructCompletions(UE4SpecifiersSource.UC, ue4Statement.Specifiers, wordSpan),
                            null));
                    break;
                case UE4Macros.UINTERFACE:
                    if (inMeta)
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UInterfaceMeta",
                            trackingSpan,
                            ConstructCompletions(
                                new[] {UE4SpecifiersSource.UMGeneric, UE4SpecifiersSource.UMI}.SelectMany(a => a)
                                    .ToArray(),
                                ue4Statement.MetaSpecifiers, wordSpan),
                            null));
                    else
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UInterface",
                            trackingSpan,
                            ConstructCompletions(UE4SpecifiersSource.UI, ue4Statement.Specifiers, wordSpan),
                            null));
                    break;
                case UE4Macros.UFUNCTION:
                    if (inMeta)
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UFunctionMeta",
                            trackingSpan,
                            ConstructCompletions(
                                new[] {UE4SpecifiersSource.UMGeneric, UE4SpecifiersSource.UMF}.SelectMany(a => a)
                                    .ToArray(),
                                ue4Statement.MetaSpecifiers, wordSpan),
                            null));
                    else
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UFunction",
                            trackingSpan,
                            ConstructCompletions(UE4SpecifiersSource.UF, ue4Statement.Specifiers, wordSpan),
                            null));
                    break;
                case UE4Macros.USTRUCT:
                    if (inMeta)
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UStructMeta",
                            trackingSpan,
                            ConstructCompletions(
                                new[] {UE4SpecifiersSource.UMGeneric, UE4SpecifiersSource.UMS}.SelectMany(a => a)
                                    .ToArray(),
                                ue4Statement.MetaSpecifiers, wordSpan),
                            null));
                    else
                    {
                        completionSets.Add(new CompletionSet(
                            "ue4",
                            "UStruct",
                            trackingSpan,
                            ConstructCompletions(UE4SpecifiersSource.US, ue4Statement.Specifiers, wordSpan),
                            null));
                    }
                    break;
            }
            session.SelectedCompletionSetChanged += SessionSelectedCompletionSetChanged;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }

        private IOrderedEnumerable<Completion> ConstructCompletions(UE4Specifier[] compList, string[] specifiers,
            SnapshotSpan trackSpan)
        {
            var currentSpecs =
                compList.Where(g => specifiers.Contains(g.Name, StringComparer.InvariantCultureIgnoreCase)).ToArray();

            return compList
                .Where(g =>
                    g.Group == null || // all not correlated Specifiers
                    currentSpecs.All(t => t.Group != g.Group) || // and not specifier with same groupId
                    currentSpecs.Contains(g) // except the one allready written
                )
                .Select(g => new Completion(g.Name, g.Name, g.Desc, null, null))
                .OrderBy(c => c.DisplayText.Contains(trackSpan.GetText()));
        }

        private void SessionSelectedCompletionSetChanged(object sender, ValueChangedEventArgs<CompletionSet> e)
        {
            var session = sender as ICompletionSession;
            if (session == null)
                return;
            if (e.OldValue == null)
            {
                var ue4Sc = session.CompletionSets.FirstOrDefault(set => set.Moniker == "ue4");
                if (ue4Sc == null)
                    return;
                session.SelectedCompletionSet = ue4Sc;
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(SnapshotPoint point, out SnapshotSpan trackgSpan)
        {
            var currentPoint = point - 1;
            var navigator = _sourceProvider.NavigatorService.GetTextStructureNavigator(_textBuffer);
            var extent = navigator.GetExtentOfWord(currentPoint);
            trackgSpan = extent.Span;
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }
    }

    [Export(typeof(ICompletionSourceProvider))]
    [Name("UE4 completion")]
    [ContentType("C/C++")]
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