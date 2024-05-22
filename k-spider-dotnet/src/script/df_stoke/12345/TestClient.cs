using System.Text.Json.Nodes;

namespace k_spider_dotnet.script.df_stoke;

public class TestClient
{
    public static void Run()
    {
        var url =
            "https://12.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=9999999&po=0&np=1&&fltt=2&invt=2&fid=f12&fs=m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048&fields=f12,f14";
        var httpClient = new HttpClient();
        // var ans = httpClient.GetAsync(url).Result;
        // Console.WriteLine(ans.Content.ReadAsStringAsync().Result);
        var responseString = new HttpClient().GetStringAsync(url).Result;
        var forecastNode = JsonNode.Parse(responseString)!;

        var jsonData = forecastNode["data"];
        if (jsonData?["diff"] == null) throw new Exception("not find date");

        var jsonDataList = (JsonArray)jsonData["diff"]!;
        foreach (var node in jsonDataList) Console.WriteLine(node["f12"].ToString());
    }
}