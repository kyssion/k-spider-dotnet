using KSpider.Strings;
using KSpider.Config;
using KSpider.Data;

namespace KSpider.Data.Devtools;

public static class PgDevelop
{
    // 更新 开发的表的信息
    public static void InitPgTableModel()
    {
        var connect = Pg.CreateConnection(new DatabaseOptions().ConnectionString);
        connect.DbFirst
            .IsCreateAttribute() //创建sqlsugar自带特性
            .StringNullable() // 字符串设置? 
            .IsCreateDefaultValue()
            .SettingConstructorTemplate(old => "") // 无构造函数
            .FormatFileName(it => StringTools.UnderlineToCamelCase(it, true) + "Model") //格式化文件名（文件名和表名不一样情况）
            .FormatClassName(it => StringTools.UnderlineToCamelCase(it, true) + "Model") //格式化类名 （类名和表名不一样的情况）
            .FormatPropertyName(it => StringTools.UnderlineToCamelCase(it, true)) //格式化属性名 （属性名和字段名不一样情况）
            .CreateClassFile("/Users/bytedance/RiderProjects/k-spider-dotnet", "KSpider.Model");
            // .CreateClassFile("/home/kyssion/project/dotnet/k-spider-dotnet", "KSpider.Model");
    }
}