namespace KSpider.Spider.DfResearchReport;

// 东方财富研报 url
public class DfStockResearchReportResource
{
    public const string StockReportUrl = "https://reportapi.eastmoney.com/report/list2";

    public struct ResearchReportQueryParameters
    {
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public string IndustryCode { get; set; }  // 默认使用 * 
        public string? RatingChange { get; set; }
        public string? Rating { get; set; }
        public string? OrgCode { get; set; }
        public string Code { get; set; } // 默认使用 *
        public string Rcode { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; } // Maps p, pageNo, pageNum, pageNumber
    }

}