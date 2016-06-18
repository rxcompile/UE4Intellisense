using System.Linq;
using Microsoft.VisualStudio.Text;

namespace UE4Intellisense.Model
{
    internal class UE4MacroStatement
    {
        public UE4MacroStatement(SnapshotSpan specifiersSpan, UE4Macros macroConst)
        {
            SpecifiersSpan = specifiersSpan;
            MacroConst = macroConst;
        }

        public UE4Macros MacroConst { get; }
        public SnapshotSpan SpecifiersSpan { get; }

        public string[] Specifiers { get; set; }
        public string[] MetaSpecifiers { get; set; }
    }

    internal class UE4Specifier
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }

    internal enum UE4Macros
    {
        UPROPERTY,
        UCLASS,
        UINTERFACE,
        UFUNCTION,
        USTRUCT
    }

    internal static class UE4Statics
    {
        public static string MacroNamesRegExPatern
            => string.Join("|", typeof(UE4Macros).GetEnumNames());
    }
}