using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace KSpider.Spider.Policy;

/// <summary>
///     政府站点正文提取共用件 : 统计局与证监会同为 TRS 建站系统 ,
///     正文容器 ( trs_editor_view / detail-news ) 内都是段落 + 表格 + font/span 混排 ,
///     文本化规则一致——容器给定后的提取逻辑集中在此 ( 未来更多官方站大概率同款 )。
/// </summary>
public static class PolicyContentExtractor
{
    /// <summary>
    ///     容器 → 纯文本 : 段落按行 , 表格按行单元格以 " | " 连接 ,
    ///     font/span 混排 ( 政府站正文的典型形态 ) 清洗为连续文本 , 空段丢弃
    /// </summary>
    public static string ExtractText(HtmlNode container)
    {
        var current = new StringBuilder();
        foreach (var node in container.SelectNodes(".//p") ?? new HtmlNodeCollection(container))
        {
            var text = Clean(node.InnerText);
            if (text != "") current.Append(text).Append('\n');
        }

        foreach (var table in container.SelectNodes(".//table") ?? new HtmlNodeCollection(container))
        {
            foreach (var row in table.SelectNodes(".//tr") ?? new HtmlNodeCollection(table))
            {
                var cells = (row.SelectNodes(".//td|./th") ?? new HtmlNodeCollection(row))
                    .Select(cell => Clean(cell.InnerText))
                    .Where(cell => cell != "")
                    .ToList();
                if (cells.Count > 0) current.Append(string.Join(" | ", cells)).Append('\n');
            }

            // 表格行已并入 , 防止 table 内的 p 被下一循环重复提取
            foreach (var p in table.SelectNodes(".//p") ?? new HtmlNodeCollection(table))
                p.Remove();
        }

        return current.ToString().TrimEnd();
    }

    /// <summary>文号提取 : "〔2026〕X 号" / "(2026) X 号" 形态 , 从标题与正文前部找 ; 提不到返回 null</summary>
    public static string? ExtractDocumentNumber(string title, string contentText)
    {
        var match = DocumentNumberRegex.Match(title) ?? DocumentNumberRegex.Match(contentText.Length > 500 ? contentText[..500] : contentText);
        return match.Success ? match.Value.Trim() : null;
    }

    /// <summary>政府站标题/条目文本的通用清洗 : 空白 ( 含换行缩进 ) 压为单空格并修剪</summary>
    public static string Clean(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? "" : WhitespaceRegex.Replace(text, " ").Trim();
    }

    /// <summary>文号形态 : 〔2026〕12 号 / (2026)3 号 / 〔2026〕-23 号 等</summary>
    private static readonly Regex DocumentNumberRegex =
        new(@"[〔（(]\s*20\d{2}\s*[〕）]\s*\d+\s*号", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
}
