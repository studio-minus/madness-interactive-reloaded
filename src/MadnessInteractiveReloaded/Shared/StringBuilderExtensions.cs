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

    public static void AppendKvStringArray(this StringBuilder b, string name, string[]? values)
    {
        if (values != null && values.Length > 0)
        {
            b.AppendLine();
            b.AppendLine(name);
            foreach (var k in values)
                b.AppendLineFormat("\t{0}", k);
        }
        b.AppendLine();
    }
}
