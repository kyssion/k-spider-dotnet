using System.Text.Json.Nodes;

namespace k_spider_dotnet.script.df_stoke;

public class DfDownloadStokeInfo
{

    // 美股  1.法拉第未来 https://65.push2.eastmoney.com/api/qt/stock/sse?fields=f58,f107,f57,f43,f59,f169,f170,f152,f46,f60,f44,f45,f47,f48,f19,f532,f39,f161,f49,f171,f50,f86,f600,f601,f154,f84,f85,f168,f108,f116,f167,f164,f92,f71,f117,f292,f301&mpi=1000&invt=2&fltt=1&secid=105.FFIE&ut=fa5fd1943c7b386f172d6893dbfba10b&wbp2u=|0|0|0|web
    //      2. 游戏驿站 https://12.push2.eastmoney.com/api/qt/stock/sse?fields=f58,f107,f57,f43,f59,f169,f170,f152,f46,f60,f44,f45,f47,f48,f19,f532,f39,f161,f49,f171,f50,f86,f600,f601,f154,f84,f85,f168,f108,f116,f167,f164,f92,f71,f117,f292,f301&mpi=1000&invt=2&fltt=1&secid=106.GME&ut=fa5fd1943c7b386f172d6893dbfba10b&wbp2u=|0|0|0|web
    // 

    // 下载所有股票id 代码https://12.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=9999999&po=0&np=1&&fltt=2&invt=2&fid=f12&fs=m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048&fields=f12,f14


    // https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=1000&fltt=2&pos=-16&secid=106.01810&wbp2u=|0|0|0|web
    public static void Test123()
    {
        var url =
            "https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=1000&fltt=2&pos=-16&secid={}.01810&wbp2u=|0|0|0|web";
        for (int a = 100; a < 1000; a++)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetStreamAsync($"https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=1000&fltt=2&pos=-16&secid={a}.01810&wbp2u=|0|0|0|web").Result;
                string ans = "";
                // 读取响应内容
                using (var reader = new System.IO.StreamReader(response))
                {
                    while (!reader.EndOfStream)
                    {
                        ans = reader.ReadLineAsync().Result ?? "";
                        break;
                    }
                }

                if (!ans.EndsWith("\"data\":null}"))
                {
                    Console.WriteLine($"find : id {a}");
                    Console.WriteLine(ans);
                    break;
                }

                Console.WriteLine($"{a} not find");
            }
        }
    }

public static void DownloadStokeInfoList2()
    {
        string content = File.ReadAllText("/home/kyssion/project/dotnet/k-spider-dotnet/k-spider-dotnet/src/script/df_stoke/stok.json");
        

        var forecastNode = JsonNode.Parse(content)!;
        var jsonData = forecastNode["data"];
        if (jsonData?["diff"] == null) throw new Exception("not find date");
        var jsonDataList = (JsonArray)jsonData["diff"]!;
        foreach (var item in jsonDataList)
        { 
            var ans = GetDatInfo22(item["f12"].ToString() ?? "").Result;
            if (ans.EndsWith("\"data\":null}"))
            {
                Console.WriteLine($"err : id {item["f12"]}");
                continue;
            }
            Console.WriteLine($" id {item["f12"]} , len {ans.Length}");
            writhFile(item["f12"].ToString(), ans);
        }
        
    }
    public static async Task<string> GetDatInfo22(string id)
    {
        using (var client = new HttpClient())
        {
            if (id == "")
            {
                return "";
            }

            string bjUrl =
                "https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid=0.{}&wbp2u=|0|0|0|web";
            string shUrl =
                "https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid=1.{}&wbp2u=|0|0|0|web";
            int number = int.Parse(id);
            var url = "";
            if (number >= 600000 && number <= 700000)
            {
                url =
                    $"https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid=1.{id}&wbp2u=|0|0|0|web";
            }
            else
            {
                url =
                    $"https://13.push2.eastmoney.com/api/qt/stock/details/sse?fields1=f1,f2,f3,f4&fields2=f51,f52,f53,f54,f55&mpi=2000&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&pos=-0&secid=0.{id}&wbp2u=|0|0|0|web";
            }

            // 连接到EventStream服务器
            var response = client.GetStreamAsync(url).Result;
            string ans = "";
            // 读取响应内容
            using (var reader = new System.IO.StreamReader(response))
            {
                while (!reader.EndOfStream)
                {
                    ans = await reader.ReadLineAsync() ?? "";
                    break;
                }
            }

            if (ans.EndsWith("\"data\":null}"))
            {
                Console.WriteLine($"err : id {id}");
            }

            return ans;
        }
    }

    public static void GetDatInfo()
    {
    }

    // 下载所有股票id 代码https://12.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=9999999&po=0&np=1&&fltt=2&invt=2&fid=f12&fs=m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048&fields=f12,f14
    public static void DownloadStokeInfoList()
    {
        var content =
            File.ReadAllText(
                "/home/kyssion/project/dotnet/k-spider-dotnet/k-spider-dotnet/src/script/df_news/spider/stok.json");


        var forecastNode = JsonNode.Parse(content)!;
        var jsonData = forecastNode["data"];
        if (jsonData?["diff"] == null) throw new Exception("not find date");
        var jsonDataList = (JsonArray)jsonData["diff"]!;
        foreach (var item in jsonDataList)
        {
            var ans = GetDatInfo22(item["f12"].ToString() ?? "").Result;
            if (ans.EndsWith("\"data\":null}"))
            {
                Console.WriteLine($"err : id {item["f12"]}");
                continue;
            }

            writhFile(item["f12"].ToString(), ans);
        }
    }

    public static void writhFile(string id, string info)
    {
        string directory = "/home/kyssion/stokinfo_20240514";
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        var file = new FileStream(directory + "/" +id, FileMode.Create);
        var w = new BinaryWriter(file);
        try
        {
            w.Write(info);
        }
        finally
        {
            file.Close();
            w.Close();
        }
    }
}


