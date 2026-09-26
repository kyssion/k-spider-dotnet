using KSpider.Spider.Verify.Model;

namespace KSpider.Spider.Verify;

/// <summary>
///     验证识别器 : 判断一次响应是不是被反爬拦住了 , 以及属于哪种验证。
///     一个识别器只认一种 ( 或一类 ) 特征 , 新增验证方式 = 新增一个实现 + 在
///     <see cref="VerificationRegistry" /> 注册一行 , 不需要改动管线与抓取层。
///     识别器必须是无状态、纯判定的 ( 不发起网络请求 ) , 否则没法离线回归。
/// </summary>
public interface IVerificationDetector
{
    /// <summary>识别器名 , 会写进日志与 <see cref="VerificationChallenge.DetectorName" /></summary>
    string Name { get; }

    /// <summary>
    ///     命中返回识别结果 , 不是自己的特征返回 null。
    ///     多个识别器同时命中时按注册顺序取第一个 —— 注册表里"特征越具体越靠前"。
    /// </summary>
    VerificationChallenge? Detect(VerificationProbe probe);
}
