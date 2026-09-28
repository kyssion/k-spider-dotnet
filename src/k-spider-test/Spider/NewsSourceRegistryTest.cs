using KSpider.Spider;
using KSpider.Spider.News.Flash.Cls;
using KSpider.Spider.News.Web.Eastmoney;
using KSpider.Spider.News.Flash;
using KSpider.Spider.News.Flash.Jin10;
using KSpider.Spider.News.Web;
using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.News.Flash.Sina;
using KSpider.Spider.News.Web.Sina;
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
    public void WebRegistryContainsEnabledSources()
    {
        // 网页型源在注册表用注释启停 ( 东财当前停用 , 财联社文章常开 ) :
        // 断言常驻源与已注册源的类型 , 不锁固定的注册集合
        Assert.IsTrue(NewsSpiderRegistry.All.Count > 0, "网页型注册表不能为空");

        var clsArticle = NewsSpiderRegistry.Get((int)FromTypeOfNews.ClsMedia);
        Assert.IsNotNull(clsArticle, "ClsMedia ( 财联社文章频道 ) 必须常驻网页型注册表");
        Assert.IsInstanceOfType<ClsArticleSpider>(clsArticle);

        var sinaArticle = NewsSpiderRegistry.Get((int)FromTypeOfNews.SinaMedia);
        Assert.IsNotNull(sinaArticle, "SinaMedia ( 新浪财经文章源 ) 必须常驻网页型注册表");
        Assert.IsInstanceOfType<SinaArticleSpider>(sinaArticle);

        // 东财按需启停 : 启用时必须是 DfNewsSpider
        var df = NewsSpiderRegistry.Get((int)FromTypeOfNews.DfMedia);
        if (df != null) Assert.IsInstanceOfType<DfNewsSpider>(df);
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
    ///     同一网站可以同时拥有两条管线 : 枚举标识"网站来源" , 管线归属由注册表决定。
    ///     财联社共用 ClsMedia —— 电报走快讯注册表 , 文章频道走网页型注册表 ;
    ///     新浪共用 SinaMedia —— 7x24 快讯走快讯注册表 , 文章源走网页型注册表
    /// </summary>
    [TestMethod]
    public void SameSiteMayOwnBothPipelines()
    {
        var clsFlash = FlashNewsSpiderRegistry.Get((int)FromTypeOfNews.ClsMedia);
        Assert.IsNotNull(clsFlash, "财联社电报应在 FlashNewsSpiderRegistry");
        Assert.IsInstanceOfType<ClsNewsSpider>(clsFlash);

        var clsWeb = NewsSpiderRegistry.Get((int)FromTypeOfNews.ClsMedia);
        Assert.IsNotNull(clsWeb, "财联社文章频道应在 NewsSpiderRegistry");
        Assert.IsInstanceOfType<ClsArticleSpider>(clsWeb);

        var sinaFlash = FlashNewsSpiderRegistry.Get((int)FromTypeOfNews.SinaMedia);
        Assert.IsNotNull(sinaFlash, "新浪 7x24 应在 FlashNewsSpiderRegistry");
        Assert.IsInstanceOfType<SinaNewsSpider>(sinaFlash);

        var sinaWeb = NewsSpiderRegistry.Get((int)FromTypeOfNews.SinaMedia);
        Assert.IsNotNull(sinaWeb, "新浪文章源应在 NewsSpiderRegistry");
        Assert.IsInstanceOfType<SinaArticleSpider>(sinaWeb);
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

        // 财联社文章频道的分类号全部落在财联社段内 ( 电报 101 在前 , 文章 102-114 接后 )
        foreach (var channel in ClsArticleResource.ArticleChannelResourceList)
        {
            var (min, max) = rangeBySource[FromTypeOfNews.ClsMedia];
            Assert.IsTrue(channel.CategoryNumber >= min && channel.CategoryNumber <= max,
                $"财联社文章频道分类号越界 : {channel.ChannelName} = {channel.CategoryNumber}");
        }

        // 新浪文章源的分类号全部落在新浪段内 ( 7x24 快讯 201 在前 , 文章 202-223 接后 )
        foreach (var column in SinaArticleResource.ArticleColumnList)
        {
            var (min, max) = rangeBySource[FromTypeOfNews.SinaMedia];
            Assert.IsTrue(column.CategoryNumber >= min && column.CategoryNumber <= max,
                $"新浪文章源分类号越界 : {column.ColumnName} = {column.CategoryNumber}");
            Assert.IsTrue(column.CategoryNumber >= 202,
                $"新浪文章源分类号不应与 7x24 快讯 ( 201 ) 冲突 : {column.ColumnName} = {column.CategoryNumber}");
        }

        // 各源编号段互不重叠
        var ranges = rangeBySource.Values.OrderBy(range => range.Min).ToList();
        for (var i = 1; i < ranges.Count; i++)
            Assert.IsTrue(ranges[i].Min > ranges[i - 1].Max,
                $"编号段重叠 : [{ranges[i - 1].Min},{ranges[i - 1].Max}] 与 [{ranges[i].Min},{ranges[i].Max}]");
    }
}
