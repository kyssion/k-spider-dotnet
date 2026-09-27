namespace KSpider.Lark;

/// <summary>
///     飞书 SDK 异常基类
/// </summary>
public class LarkExperiment : Exception
{
}

/// <summary>
///     token 获取失败 , 或消息发送返回错误 code 时抛出
/// </summary>
public class LarkGetTokenError : LarkExperiment{}
