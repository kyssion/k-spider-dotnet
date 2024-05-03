using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.model;
using k_spider_dotnet.model.ressource;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.script.df_news.spider.playwright;
using k_spider_dotnet.tool.log;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfListSpiderTest
{
    private readonly ILogger _logger = LogFactory.GetLogger<DfListSpiderTest>();

}