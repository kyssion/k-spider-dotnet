using System.Text;
namespace KSpider.Strings;

public class StringTools
{
    // 下划线转化成驼峰 
    public static string UnderlineToCamelCase(string name, bool firstIsCapitalized)
    {
        var builder = new StringBuilder();
        foreach (var i in name)
        {
            if (i == '_')
            {
                firstIsCapitalized = true;
                continue;
            }

            builder.Append(firstIsCapitalized ? ChatToUpper(i) : i);
            firstIsCapitalized = false;
        }

        return builder.ToString();
    }

    public static char ChatToLower(char i)
    {
        if (i is >= 'A' and <= 'Z') return (char)('a' + i - 'A');
        return i;
    }

    public static char ChatToUpper(char i)
    {
        if (i is >= 'a' and <= 'z') return (char)('A' + i - 'a');
        return i;
    }
}