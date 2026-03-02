// This shim allows using C# 9+ 'init' property accessors when targeting netstandard2.1.
// See: https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1285582
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
