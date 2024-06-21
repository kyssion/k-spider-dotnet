using k_spider_dotnet_lib.lark;
using k_spider_dotnet.job.larkJob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.tool.lark;

[TestClass]
public class LarkTest
{
    [TestMethod]
    public void TestSendMessageByJob()
    {
        Console.WriteLine("start");
       new SendLarkNewsMessageJob().SendNewsMessage();
    }
    [TestMethod]
    public void TestSendMessage()
    {
        Console.WriteLine("start");
        LarkMessage.SendTemplateMessage("cli_a6c6ce8d66fa500e", "gX9w2dWfiX9cvHm4oCvR7eHG7lDDJqD2",
            "chat_id", "oc_43cfa41c91def10da22305278eb4de3e", new LarkMessage.TemplateInfo
            {
                Data = new LarkMessage.TemplateData
                {
                    TemplateId = "ctp_AAk5g6Ps48sP",
                    TemplateVariable =new Dictionary<string, object>()
                    {
                       { "news_title" , "菲仕兰：半乳糖基乳糖可促进婴儿特异双歧杆菌生长"},
                       { "news_summary", "新京报记者5月22日获悉，在“第三届中国母乳科学大会”上，荷兰皇家菲仕兰营养与健康高级研究员DianneDelsing分享“多样化的低聚糖如何影响婴儿的健康”报告，称母乳低聚糖（HMOs）、低聚半乳糖（GOS）、半乳糖基乳糖（GLs）等成分有助于婴幼儿肠道健康，多样化的低聚糖被越来越多地用于婴幼儿营养补充。"},
                       { "from_media", "东方财富"},
                       { "news_from", "新京报"},
                       { "news_time", "2024-05-22 14:51:10"},
                       { "news_url", "http://finance.eastmoney.com/news/1355,202405223084174751.html"}
                    }
                }
            }).Wait();
    }
}