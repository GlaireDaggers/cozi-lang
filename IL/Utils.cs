namespace Cozi.IL
{
    public static class Utils
    {
        public static string EscapeString(string str)
        {
            return str.Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("\0", "\\0")
                .Replace("\a", "\\a")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\v", "\\v");
        }
    }
}