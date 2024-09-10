using System.Text;

namespace MIR;

public static class StringBuilderExtensions
{
    public static void AppendLineFormat(this StringBuilder b, string format, object? arg)
    {
        b.AppendFormat(format, arg);
        b.AppendLine();
    }  
    
    public static void AppendLineFormat(this StringBuilder b, string format, params object?[] arg)
    {
        b.AppendFormat(format, arg);
        b.AppendLine();
    }
}
