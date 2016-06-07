namespace UE4Intellisense
{
    internal class UE4Statement
    {
        public UE4Macros MacroConst { get; set; }
        public string[] Specifiers { get; set; }
        public string[] MetaSpecifiers { get; set; }
    }

    internal class UE4Specifier
    {
        public int? GroupId { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
    }

    internal enum UE4Macros
    {
        UProperty,
        UClass,
        UInterface,
        UFunction,
        UStruct
    }
}
