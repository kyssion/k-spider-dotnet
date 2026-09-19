using KSpider.Job.News;
using KSpider.Model;
using KSpider.Spider;
using KSpider.Spider.ClsNews;
using KSpider.Spider.DfNews;
using KSpider.Spider.Jin10News;
using KSpider.Spider.News;
using KSpider.Spider.SinaNews;
using KSpider.Spider.WscnNews;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     多源注册表与流水线状态置位规则测试 ( 全部离线 , 不访问网络 )
/// </summary>
[TestClass]
public class NewsSourceRegistryTest
{
    /// <summary>
    ///     每个已接入的源都必须注册且 from_media 与枚举一致
    /// </summary>
    [TestMethod]
    public void RegistryRegisterAllImplementedSources()
    {
        var expected = new (FromTypeOfNews FromMedia, Type SpiderType, int Category)[]
        {
            (FromTypeOfNews.DfMedia, typeof(DfNewsSpider), 0),
            (FromTypeOfNews.ClsMedia, typeof(ClsNewsSpider), 101),
            (FromTypeOfNews.SinaMedia, typeof(SinaNewsSpider), 201),
            (FromTypeOfNews.WscnMedia, typeof(WscnNewsSpider), 301),
            (FromTypeOfNews.Jin10Media, typeof(Jin10NewsSpider), 401)
        };

        foreach (var (fromMedia, spiderType, _) in expected)
        {
            var spider = NewsSpiderRegistry.Get((int)fromMedia);
            Assert.IsNotNull(spider, $"{fromMedia} 未在 NewsSpiderRegistry 注册");
            Assert.AreEqual(fromMedia, spider.FromMedia, $"{fromMedia} 的 FromMedia 不匹配");
            Assert.AreEqual(spiderType, spider.GetType(), $"{fromMedia} 注册的实现类型不符");
            Assert.IsTrue(spider.Columns.Count > 0, $"{fromMedia} 没有配置任何栏目");
        }

        Assert.AreEqual(expected.Length, NewsSpiderRegistry.All.Count);
    }

    /// <summary>
    ///     每个源的栏目必须有非空标识与名称 ( 列表任务与健康检查都依赖 )
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
    }

    /// <summary>
    ///     各源 category 必须落在各自的编号段内 , 不能互相串用
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

        // 各源分类号常量必须落在自己的编号段内
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

    /// <summary>
    ///     快讯型源 ( 列表即全文 ) 的列表项必须带原始内容 , 且状态会被置为已下载 ;
    ///     东财是详情页型源 , 不带内联原始内容 , 状态保持未下载
    /// </summary>
    [TestMethod]
    public void MarkInlineOriginItemsOnlyAffectFastNewsSources()
    {
        // 东财 : 无内联原始内容 , 状态保持 0
        var dfPage = new NewsListPage
        {
            Items = [new SpiderNewsListModel { NewsUrl = "https://finance.eastmoney.com/a/1.html" }]
        };
        NewsListJob.MarkInlineOriginItems(dfPage);
        Assert.AreEqual((int)NewsDownloadStatusCode.NoDownload, dfPage.Items[0].DownloadStatusCode);

        // 快讯型 : 有内联原始内容 , 状态置为 3 ( 跳过下载阶段 )
        var fastPage = new NewsListPage
        {
            Items = [new SpiderNewsListModel { NewsUrl = "https://www.cls.cn/detail/1" }],
            InlineOrigins =
            [
                new NewsContentOrigin
                {
                    NewsUrl = "https://www.cls.cn/detail/1",
                    Status = NewsContentOriginStatus.Success
                }
            ]
        };
        NewsListJob.MarkInlineOriginItems(fastPage);
        Assert.AreEqual((int)NewsDownloadStatusCode.SuccessDownloadOriginInfo, fastPage.Items[0].DownloadStatusCode);
    }

    /// <summary>
    ///     原始内容与列表项不匹配时不能误置状态 ( 避免产生"已下载却没有 origin"的悬空行 )
    /// </summary>
    [TestMethod]
    public void MarkInlineOriginItemsSkipUrlMismatch()
    {
        var page = new NewsListPage
        {
            Items =
            [
                new SpiderNewsListModel { NewsUrl = "https://www.cls.cn/detail/1" },
                new SpiderNewsListModel { NewsUrl = "https://www.cls.cn/detail/2" }
            ],
            InlineOrigins =
            [
                new NewsContentOrigin { NewsUrl = "https://www.cls.cn/detail/1" }
            ]
        };

        NewsListJob.MarkInlineOriginItems(page);

        Assert.AreEqual((int)NewsDownloadStatusCode.SuccessDownloadOriginInfo, page.Items[0].DownloadStatusCode);
        Assert.AreEqual((int)NewsDownloadStatusCode.NoDownload, page.Items[1].DownloadStatusCode);
    }

    /// <summary>
    ///     快讯型源的 GetContentOrigin 必须返回失败态 ( 没有按 id 重拉的接口 , 不能假装成功 )
    /// </summary>
    [TestMethod]
    public void FastNewsSourceContentOriginReportFailed()
    {
        var newsItem = new SpiderNewsListModel { NewsUrl = "https://example.com/detail/1" };
        foreach (var fromMedia in new[]
                     { FromTypeOfNews.ClsMedia, FromTypeOfNews.SinaMedia, FromTypeOfNews.WscnMedia, FromTypeOfNews.Jin10Media })
        {
            var origin = NewsSpiderRegistry.Get((int)fromMedia)!.GetContentOrigin(newsItem).GetAwaiter().GetResult();

            Assert.AreEqual(NewsContentOriginStatus.Failed, origin.Status, $"{fromMedia} 应返回失败态");
            Assert.AreEqual("", origin.NewsOriginContent);
            Assert.IsFalse(string.IsNullOrWhiteSpace(origin.Message), $"{fromMedia} 失败态应带说明");
        }
    }
}
