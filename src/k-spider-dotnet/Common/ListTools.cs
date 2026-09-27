namespace KSpider.Common;

public class ListTools
{
    /// <summary>
    ///     按 chunkSize 切成若干段 ( 末段可能不满 ) , 空列表返回空结果
    /// </summary>
    public static List<List<T>> Partition<T>(List<T> lists, int chunkSize)
    {
        var ansList = new List<List<T>>();
        for (var i = 0; i < lists.Count; i += chunkSize)
            ansList.Add(lists.GetRange(i, Math.Min(chunkSize, lists.Count - i)));

        return ansList;
    }
}
