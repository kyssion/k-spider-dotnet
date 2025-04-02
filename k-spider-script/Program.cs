using k_spirder_script.script;

namespace k_spirder_script;

internal static class Program
{
// 定义一个静态方法Main，这是C#程序的入口点
    static void Main(string[] args)
    {
        Console.WriteLine("start......");
        TransferSiderData.DoTransfer();
        Console.WriteLine("end......");
    }
}