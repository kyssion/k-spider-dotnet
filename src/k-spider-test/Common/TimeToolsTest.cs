using Microsoft.VisualStudio.TestTools.UnitTesting;

using KSpider.Common;
namespace KSpider.Test.Common;

/// <summary>
///     TimeTools 时间解析与差值计算回归 ( 全部离线 )
/// </summary>
[TestClass]
public class TimeToolsTest
{
    [TestMethod]
    public void GetDateByBackSlashFormat()
    {
        var date = TimeTools.GetDateByTimeStrForFormat("2026/09/06 10:30:00", TimeTools.TimeFormatForBackSlash);
        Assert.AreEqual(new DateTime(2026, 9, 6, 10, 30, 0), date);
    }

    [TestMethod]
    public void GetDateByStrikethroughFormat()
    {
        var date = TimeTools.GetDateByTimeStrForFormat("2026-09-06 10:30:00", TimeTools.TimeFormatForStrikethrough);
        Assert.AreEqual(new DateTime(2026, 9, 6, 10, 30, 0), date);
    }

    [TestMethod]
    public void GetDateThrowOnFormatMismatch()
    {
        // 列表接口与详情接口返回的时间格式不同 , 格式不匹配会直接抛异常 , 调用方需保证格式正确
        Assert.ThrowsExactly<FormatException>(() =>
            TimeTools.GetDateByTimeStrForFormat("2026/09/06 10:30:00", TimeTools.TimeFormatForStrikethrough));
    }

    [TestMethod]
    public void GetDiffInSecondsPositive()
    {
        var diff = TimeTools.GetDiffInSeconds(new DateTime(2026, 9, 6, 0, 1, 0), new DateTime(2026, 9, 6, 0, 0, 0));
        Assert.AreEqual(60d, diff);
    }
}
