using k_spider_dotnet_lib.@string;
using k_spider_dotnet.dal.db;

namespace k_spider_dotnet.db.postgresql;

public static class PgDevelop
{
    // 更新 开发的表的信息
    public static void InitPgTableModel()
    {
        var connect = Pg.Connection();
        connect.DbFirst
            .IsCreateAttribute() //创建sqlsugar自带特性
            .StringNullable() // 字符串设置? 
            .IsCreateDefaultValue()
            .SettingConstructorTemplate(old => "") // 无构造函数
            .FormatFileName(it => StringTools.UnderlineToCamelCase(it, true) + "Model") //格式化文件名（文件名和表名不一样情况）
            .FormatClassName(it => StringTools.UnderlineToCamelCase(it, true) + "Model") //格式化类名 （类名和表名不一样的情况）
            .FormatPropertyName(it => StringTools.UnderlineToCamelCase(it, true)) //格式化属性名 （属性名和字段名不一样情况）
            .CreateClassFile("/Users/bytedance/RiderProjects/k-spider-dotnet", "k_spider_dotnet.model");
            // .CreateClassFile("/home/kyssion/project/dotnet/k-spider-dotnet", "k_spider_dotnet.model");
    }
}