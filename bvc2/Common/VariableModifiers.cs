namespace bvc2.Common;

[Flags]
enum VariableModifiers
{
    None = 0,
    Val = 1 << 0,
    Static = 1 << 1,
}
