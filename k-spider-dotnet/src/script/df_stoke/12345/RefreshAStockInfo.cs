using System.Text.Json.Nodes;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.model;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.script.df_stoke._12345;

public class RefreshAStockInfo
{
    private const string Url =
        "https://12.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=9999999&po=0&np=1&&fltt=2&invt=2&fid=f12&fs=m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048&fields=f12,f14";

    private static readonly ILogger Logger = LogFactory.GetLogger<RefreshAStockInfo>();

    public static void RefreshExchangeChannel()
    {
        var httpClient = new HttpClient();
        // var ans = httpClient.GetAsync(url).Result;
        // Console.WriteLine(ans.Content.ReadAsStringAsync().Result);
        var responseString = new HttpClient().GetStringAsync(Url).Result;
        var forecastNode = JsonNode.Parse(responseString)!;

        var jsonData = forecastNode["data"];
        if (jsonData?["diff"] == null) throw new Exception("not find date");

        var jsonDataList = (JsonArray)jsonData["diff"]!;
        var stockList = new List<StockCnIntroductionModel>();
        foreach (var nodeItem in jsonDataList)
        {
            var stockId = nodeItem?["f12"]?.ToString() ?? "";
            if (stockId == "")
            {
                Logger.LogError("[RefreshAStockInfo] refresh , stockID is empty");
                continue;
            }

            stockList.Add(new StockCnIntroductionModel
            {
                StockId = stockId,
                StockName = nodeItem?["f14"]?.ToString() ?? "",
                ExchangeChannel = (int)(stockId.StartsWith("6") ? StockExchangeChannel.ShangHStockExchangeChannel : StockExchangeChannel.SzBjStockExchangeChannel)
            });
        }

        using var connection = Pg.Connection();
        StockDao.UpsetStockCnIntroduction(connection, stockList);
    }
}

public class RefreshHkStockInfo
{
    private const string Url =
        "http://56.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=9999999&po=0&np=1&&fltt=2&invt=2&wbp2u=|0|0|0|web&fid=f12&fs=m:128+t:3,m:128+t:4,m:128+t:1,m:128+t:2&fields=f12,f13";

    private static readonly ILogger Logger = LogFactory.GetLogger<RefreshAStockInfo>();

    public static void RefreshExchangeChannel()
    {
        var httpClient = new HttpClient();
        // var ans = httpClient.GetAsync(url).Result;
        // Console.WriteLine(ans.Content.ReadAsStringAsync().Result);
        var responseString = new HttpClient().GetStringAsync(Url).Result;
        var forecastNode = JsonNode.Parse(responseString)!;

        var jsonData = forecastNode["data"];
        if (jsonData?["diff"] == null) throw new Exception("not find date");

        var jsonDataList = (JsonArray)jsonData["diff"]!;
        var stockList = new List<StockCnIntroductionModel>();
        foreach (var nodeItem in jsonDataList)
        {
            var stockId = nodeItem?["f12"]?.ToString() ?? "";
            if (stockId == "")
            {
                Logger.LogError("[RefreshAStockInfo] refresh , stockID is empty");
                continue;
            }

            stockList.Add(new StockCnIntroductionModel
            {
                StockId = stockId,
                StockName = nodeItem?["f14"]?.ToString() ?? "",
                ExchangeChannel = (int)(stockId.StartsWith("8") || stockId.StartsWith("4")
                    ? StockExchangeChannel.ShangHStockExchangeChannel
                    : StockExchangeChannel.SzBjStockExchangeChannel)
            });
        }

        using var connection = Pg.Connection();
        StockDao.UpsetStockCnIntroduction(connection, stockList);
    }
}