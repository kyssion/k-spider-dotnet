# k-spider-dotnet


7 x24 小时实时数据 :"

https://finance.sina.com.cn/7x24/?tag=0
https://www.gelonghui.com/live/
https://wallstreetcn.com/live/global
https://36kr.com/newsflashes/
https://kuaixun.eastmoney.com/
https://www.jin10.com/

# 

1. 已经完成了一个新闻拉取聚合的能力
2. 需要一个页面的可以方便录入需要的关注的股票和市场信

1. 整体宏观趋势 -  A股 港股 美股 日股
2. 个股监控
   curl 'https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid=0.600000&wbp2u=|0|0|0|web' \
   -H 'Accept: text/event-stream' 