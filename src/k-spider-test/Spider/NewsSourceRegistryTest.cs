using KSpider.Spider;
using KSpider.Spider.News.Flash.Cls;
using KSpider.Spider.News.Web.Eastmoney;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Flash.Jin10;
using KSpider.Spider.News.Web;
using KSpider.Spider.News.Flash.Sina;
using KSpider.Spider.News.Flash.Wscn;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     双注册表测试 : 网页抓取型 ( NewsSpiderRegistry ) 与实时快讯型 ( FlashNewsSpiderRegistry )
///     互相独立 , 一个源只能属于一种管线 ( 全部离线 )
/// </summary>
[TestClass]
public class NewsSourceRegistryTest
{
    [TestMethod]
    public void WebRegistryOnlyContainsDfSource()
    {
        var spider = NewsSpiderRegistry.Get((int)FromTypeOfNews.DfMedia);

        Assert.IsNotNull(spider);
        Assert.IsInstanceOfType<DfNewsSpider>(spider);
        Assert.AreEqual(1, NewsSpiderRegistry.All.Count);
    }

    [TestMethod]
    public void FlashRegistryContainsAllFlashSources()
    {
        var expected = new (FromTypeOfNews FromMedia, Type SpiderType)[]
        {
            (FromTypeOfNews.ClsMedia, typeof(ClsNewsSpider)),
            (FromTypeOfNews.SinaMedia, typeof(SinaNewsSpider)),
            (FromTypeOfNews.WscnMedia, typeof(WscnNewsSpider)),
            (FromTypeOfNews.Jin10Media, typeof(Jin10NewsSpider))
        };

        foreach (var (fromMedia, spiderType) in expected)
        {
            var spider = FlashNewsSpiderRegistry.Get((int)fromMedia);
            Assert.IsNotNull(spider, $"{fromMedia} 未在 FlashNewsSpiderRegistry 注册");
            Assert.AreEqual(spiderType, spider.GetType());
            Assert.IsTrue(spider.Columns.Count > 0, $"{fromMedia} 没有配置任何栏目");
        }

        Assert.AreEqual(expected.Length, FlashNewsSpiderRegistry.All.Count);
    }

    /// <summary>
    ///     一个源只能属于一种管线 : 快讯源不能出现在网页型注册表里 , 反之亦然
    /// </summary>
    [TestMethod]
    public void SourceBelongsToExactlyOnePipeline()
    {
        foreach (var spider in FlashNewsSpiderRegistry.All)
            Assert.IsNull(NewsSpiderRegistry.Get((int)spider.FromMedia),
                $"{spider.FromMedia} 不应同时注册在两种管线里");
        foreach (var spider in NewsSpiderRegistry.All)
            Assert.IsNull(FlashNewsSpiderRegistry.Get((int)spider.FromMedia),
                $"{spider.FromMedia} 不应同时注册在两种管线里");
    }

    [TestMethod]
    public void RegistryReturnNullForUnknownSource()
    {
        Assert.IsNull(NewsSpiderRegistry.Get(999));
        Assert.IsNull(FlashNewsSpiderRegistry.Get(999));
    }

    /// <summary>
    ///     每个源的栏目必须有非空标识与名称 ( 任务与健康检查都依赖 )
    /// </summary>
    [TestMethod]
    public void EverySourceColumnHasIdAndName()
    {
        foreach (var spider in NewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.ColumnId), $"{spider.FromMedia} 存在空栏目号");
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.ColumnName), $"{spider.FromMedia} 存在空栏目名");
        }

        foreach (var spider in FlashNewsSpiderRegistry.All)
        foreach (var column in spider.Columns)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.ColumnId), $"{spider.FromMedia} 存在空栏目号");
            Assert.IsFalse(string.IsNullOrWhiteSpace(column.ColumnName), $"{spider.FromMedia} 存在空栏目名");
        }
    }

    /// <summary>
    ///     各源 category 必须落在各自的编号段内 , 编号段互不重叠
    /// </summary>
    [TestMethod]
    public void SourceCategoryUsesOwnNumberRange()
    {
        var rangeBySource = new Dictionary<FromTypeOfNews, (int Min, int Max)>
        {
            [FromTypeOfNews.DfMedia] = (1, 22),
            [FromTypeOfNews.ClsMedia] = (101, 199),
            [FromTypeOfNews.SinaMedia] = (201, 299),
            [FromTypeOfNews.WscnMedia] = (301, 399),
            [FromTypeOfNews.Jin10Media] = (401, 499)
        };

        var sourceCategory = new Dictionary<FromTypeOfNews, int>
        {
            [FromTypeOfNews.ClsMedia] = ClsNewsResource.TelegraphCategoryNumber,
            [FromTypeOfNews.SinaMedia] = SinaNewsResource.LiveCategoryNumber,
            [FromTypeOfNews.WscnMedia] = WscnNewsResource.LiveCategoryNumber,
            [FromTypeOfNews.Jin10Media] = Jin10NewsResource.FlashCategoryNumber
        };
        foreach (var (fromMedia, category) in sourceCategory)
        {
            var (min, max) = rangeBySource[fromMedia];
            Assert.IsTrue(category >= min && category <= max, $"{fromMedia} 分类号越界 : {category}");
        }

        // 东财 35 个栏目的分类号全部落在 1-22 内
        foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
        {
            var number = resourceItem.CategoryInfo.CategoryNumber;
            var (min, max) = rangeBySource[FromTypeOfNews.DfMedia];
            Assert.IsTrue(number >= min && number <= max, $"东财栏目分类号越界 : {number}");
        }

        // 各源编号段互不重叠
        var ranges = rangeBySource.Values.OrderBy(range => range.Min).ToList();
        for (var i = 1; i < ranges.Count; i++)
            Assert.IsTrue(ranges[i].Min > ranges[i - 1].Max,
                $"编号段重叠 : [{ranges[i - 1].Min},{ranges[i - 1].Max}] 与 [{ranges[i].Min},{ranges[i].Max}]");
    }
}
