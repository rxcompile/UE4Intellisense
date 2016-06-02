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
        private List<Completion> m_compListProps;
        private List<Completion> m_compListClass;
        private List<Completion> m_compListFunc;
        private List<Completion> m_compListStruct;
        private List<Completion> m_compListIface;

        public UE4SpecifiersCompletionSource(UE4SpecifiersCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;

            m_compListProps = @"
AdvancedDisplay
AssetRegistrySearchable
BlueprintAssignable
BlueprintCallable
BlueprintReadOnly
BlueprintReadWrite
Category
Config
Const
DuplicateTransient
EditAnywhere
EditDefaultsOnly
EditFixedSize
EditInline
EditInstanceOnly
Export
GlobalConfig
Instanced
Interp
Localized
Native
NoClear
NoExport
NonTransactional
Ref
Replicated
ReplicatedUsing
RepRetry
SaveGame
SerializeText
SimpleDisplay
Transient
VisibleAnywhere
VisibleDefaultsOnly
VisibleInstanceOnly".Split(new char[] { '\n' })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Select(s => new Completion(s, s, s, null, null)).ToList();

            m_compListClass = @"
Abstract
AdvancedClassDisplay
AutoCollapseCategories
AutoExpandCategories
Blueprintable
BlueprintType
ClassGroup
CollapseCategories
Config
Const
ConversionRoot
CustomConstructor
DefaultToInstanced
DependsOn
Deprecated
DontAutoCollapseCategories
DontCollapseCategories
EditInlineNew
HideCategories
HideDropdown
HideFunctions
Intrinsic
MinimalAPI
NoExport
NonTransient
NotBlueprintable
NotPlaceable
PerObjectConfig
Placeable
ShowCategories
ShowFunctions
Transient
Within".Split(new char[] { '\n' })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Select(s => new Completion(s, s, s, null, null)).ToList();

            m_compListFunc = @"
BlueprintAuthorityOnly
BlueprintCallable
BlueprintCosmetic
BlueprintImplementableEvent
BlueprintNativeEvent
BlueprintPure
Category
Client
CustomThunk
Exec
NetMulticast
Reliable
Server
Unreliable".Split(new char[] { '\n' })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Select(s => new Completion(s, s, s, null, null)).ToList();

            m_compListStruct = @"
Atomic
BlueprintType
Immutable
NoExport".Split(new char[] { '\n' })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Select(s => new Completion(s, s, s, null, null)).ToList();

            m_compListIface = @"
Blueprintable
DependsOn
MinimalAPI
NotBlueprintable".Split(new char[] { '\n' })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Select(s => new Completion(s, s, s, null, null)).ToList();
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            var triggerPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot).GetValueOrDefault();

            if (IsTrackUE4MacroSpecifier(triggerPoint, "UPROPERTY"))
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UProperty",
                  FindTokenSpanAtPosition(triggerPoint),
                  m_compListProps,
                  null);

                completionSets.Add(ue4cs);
            }
            if (IsTrackUE4MacroSpecifier(triggerPoint, "UCLASS"))
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UClass",
                  FindTokenSpanAtPosition(triggerPoint),
                  m_compListClass,
                  null);

                completionSets.Add(ue4cs);
            }
            if (IsTrackUE4MacroSpecifier(triggerPoint, "UINTERFACE"))
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UInterface",
                  FindTokenSpanAtPosition(triggerPoint),
                  m_compListIface,
                  null);

                completionSets.Add(ue4cs);
            }
            if (IsTrackUE4MacroSpecifier(triggerPoint, "UFUNCTION"))
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UFunction",
                  FindTokenSpanAtPosition(triggerPoint),
                  m_compListFunc,
                  null);

                completionSets.Add(ue4cs);
            }
            if (IsTrackUE4MacroSpecifier(triggerPoint, "USTRUCT"))
            {
                var ue4cs = new CompletionSet(
                  "ue4",
                  "UStruct",
                  FindTokenSpanAtPosition(triggerPoint),
                  m_compListStruct,
                  null);

                completionSets.Add(ue4cs);
            }
            session.SelectedCompletionSetChanged += SessionSelectedCompletionSetChanged;
        }
        
        private bool IsTrackUE4MacroSpecifier(SnapshotPoint triggerPoint, string macroConst)
        {
            SnapshotPoint currentPoint = triggerPoint - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);

            SnapshotSpan statement = navigator.GetSpanOfEnclosing(extent.Span);
            var statementText = statement.GetText();
            var match = Regex.Match(statementText, $@"{macroConst}\((.*)\)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;
            if (!match.Groups[1].Success)
                return false;

            var contentPosition = statement.Start + match.Groups[1].Index;
            var contentEnd = contentPosition + match.Groups[1].Length;

            if (extent.Span.Start < contentPosition || extent.Span.End > contentEnd)
                return false;

            return true;
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
