using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace k_spider_dotnet.model
{
    ///<summary>
    ///排重抓取信息信息列表
    ///</summary>
    [SugarTable("spider_news_list")]
    public partial class SpiderNewsListModel
    {
           public SpiderNewsListModel(){


           }
           /// <summary>
           /// Desc:
           /// Default:nextval('spider_news_list_id_seq'::regclass)
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true,ColumnName="id")]
           public long Id {get;set;}

           /// <summary>
           /// Desc:
           /// Default:DateTime.Now
           /// Nullable:False
           /// </summary>
           [SugarColumn(ColumnName="create_time")]           
           public DateTime CreateTime {get;set;}

           /// <summary>
           /// Desc:
           /// Default:DateTime.Now
           /// Nullable:False
           /// </summary>
           [SugarColumn(ColumnName="update_time")]           
           public DateTime UpdateTime {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="from_media")]           
           public int? FromMedia {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_url")]           
           public string NewsUrl {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_title")]           
           public string NewsTitle {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_summary")]           
           public string NewsSummary {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_from")]           
           public string NewsFrom {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_time")]           
           public DateTime? NewsTime {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="news_download_time")]           
           public DateTime? NewsDownloadTime {get;set;}

           /// <summary>
           /// Desc:新闻类型
           /// Default:0
           /// Nullable:True
           /// </summary>
           [SugarColumn(ColumnName="category")]           
           public int? Category {get;set;}

    }
}
