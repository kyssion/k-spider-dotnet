using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.exception;
using k_spider_dotnet.job.dfNewsJob;
using k_spider_dotnet.model;
using k_spider_dotnet.model.ressource;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlSugar;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfContentSpiderTest
{
    private readonly ILogger _logger = LogFactory.GetLogger<DfContentSpiderTest>();

}