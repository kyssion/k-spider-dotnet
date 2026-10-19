namespace KSpider.Lark;

/// <summary>
///     飞书开放平台接口地址基类 : 各客户端继承以复用 URL 常量
/// </summary>
public class LarkDatasource
{
    protected static readonly string TenantAccessTokenUrl = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
    protected static readonly string SendMessageUrl = "https://open.feishu.cn/open-apis/im/v1/messages";
}
