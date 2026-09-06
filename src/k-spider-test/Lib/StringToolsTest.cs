using KSpider.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Lib;

[TestClass]
public class StringToolsTest
{
    [TestMethod]
    public void UnderlineToCamelCaseFirstNotCapitalized()
    {
        Assert.AreEqual("spiderNewsList", StringTools.UnderlineToCamelCase("spider_news_list", false));
    }

    [TestMethod]
    public void UnderlineToCamelCaseFirstCapitalized()
    {
        Assert.AreEqual("SpiderNewsList", StringTools.UnderlineToCamelCase("spider_news_list", true));
    }

    [TestMethod]
    public void UnderlineToCamelCaseNoUnderline()
    {
        // 无下划线时只处理首字符 , 不会给单词内部大写
        Assert.AreEqual("Newsurl", StringTools.UnderlineToCamelCase("newsurl", true));
        Assert.AreEqual("newsurl", StringTools.UnderlineToCamelCase("newsurl", false));
    }

    [TestMethod]
    public void UnderlineToCamelCaseEmptyString()
    {
        Assert.AreEqual("", StringTools.UnderlineToCamelCase("", true));
    }

    [TestMethod]
    public void ChatToUpperLowercaseToUppercase()
    {
        Assert.AreEqual('A', StringTools.ChatToUpper('a'));
    }

    [TestMethod]
    public void ChatToUpperNonLetterUnchanged()
    {
        Assert.AreEqual('Z', StringTools.ChatToUpper('Z'));
        Assert.AreEqual('1', StringTools.ChatToUpper('1'));
        Assert.AreEqual('中', StringTools.ChatToUpper('中'));
    }

    [TestMethod]
    public void ChatToLowerUppercaseToLowercase()
    {
        Assert.AreEqual('a', StringTools.ChatToLower('A'));
    }

    [TestMethod]
    public void ChatToLowerNonLetterUnchanged()
    {
        Assert.AreEqual('a', StringTools.ChatToLower('a'));
        Assert.AreEqual('2', StringTools.ChatToLower('2'));
        Assert.AreEqual('文', StringTools.ChatToLower('文'));
    }
}
