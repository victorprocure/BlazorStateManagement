using BlazorStateManagement.Core;

namespace BlazorStateManagement.Common;
internal static class DebuggerDisplayFormatting
{
    internal static string DebuggerToString(string name, IState state)
    {
        var debugText = $@"Name = ""{name}""";
        debugText += $"{Environment.NewLine}{state}";

        return debugText;
    }
}
