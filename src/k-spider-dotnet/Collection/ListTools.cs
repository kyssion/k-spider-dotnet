namespace KSpider.Collection;

public class ListTools
{
    public static List<List<T>> Partition<T>(List<T> lists, int chunkSize)
    {
        var ansList = new List<List<T>>();
        for (var i = 0; i < lists.Count; i += chunkSize)
            ansList.Add(lists.GetRange(i, Math.Min(chunkSize, lists.Count - i)));

        return ansList;
    }
}