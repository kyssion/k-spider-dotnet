using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Python.Runtime;

namespace k_spider_dotnet_test;

[TestClass]
public class DemoTest
{
    public static void PrintCurrentMethodName()
    {
        // 获取当前方法的信息
        var method = MethodBase.GetCurrentMethod();

        // 打印当前方法的完全限定名称
        Console.WriteLine(method.DeclaringType.FullName + "." + method.Name);
    }
    
    [TestMethod]
    public void TestPython()
    {
        PrintCurrentMethodName();
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/Users/bytedance/miniconda3/envs/pytorch/lib/libpython3.10.dylib");
        PythonEngine.Initialize();
        using (Py.GIL()) // 初始化 Python 运行时并获取全局解释器锁
        {
            var sys = Py.Import("sys");
            Console.WriteLine(sys.GetAttr("path"));
            Console.WriteLine("------------------------");
            dynamic np = Py.Import("numpy");
            dynamic result = np.sin(new int[]{123,444});  // 使用NumPy的sin函数计算结果
            Console.WriteLine(result);
        }
    }
    
   // import hanlp
   //  HanLP = hanlp.pipeline() 114.253.38.211
   //      .append(hanlp.utils.rules.split_sentence, output_key='sentences') 
   //      .append(hanlp.load('FINE_ELTRA_SM114.253.38.211ALL_ZH'), output_key='tok') 
   //      .append(hanlp.load('CTB9_POS_ELECTRA_SMALL'), output_key='pos') 
   //      .append(hanlp.load('MSRA_NER_ELECTRA_SMALL_ZH'), output_key='ner', input_key='tok') 
   //      .append(hanlp.load('CTB9_DEP_ELECTRA_SMALL', conll=0), output_key='dep', input_key='tok')
   //      .append(hanlp.load('CTB9_CON_ELECTRA_SMALL'), output_key='con', input_key='tok'
   //  print(HanLP('''据360公司官微消息，6月6日，360AI新品发布会暨开发者沟通会在京举办，会员体系“360AI大会员”正式上线。360AI大会员体系采用会员订阅模式，该会员服务覆盖图片、写作、文档、视频、文档模板等五大场景100多款实用工具，可通过360旗下多款浏览器开通“360AI大会员”，即可解锁全部应用。
   // '''))
    [TestMethod]
    public void TestJiebaPython()
    {
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/Users/bytedance/miniconda3/envs/pytorch/lib/libpython3.10.dylib");
        PythonEngine.Initialize();
        using (Py.GIL()) // 初始化 Python 运行时并获取全局解释器锁
        {
            dynamic pkuseg = Py.Import("pkuseg");
            dynamic seg = pkuseg.pkuseg("default", "/Users/bytedance/RiderProjects/k-spider-dotnet/k-spider-dotnet-test/dic", false);
            dynamic info = seg.cut("据360公司官微消息，6月6日，360AI新品发布会暨开发者沟通会在京举办，会员体系“360AI大会员”正式上线。360AI大会员体系采用会员订阅模式，该会员服务覆盖图片、写作、文档、视频、文档模板等五大场景100多款实用工具，可通过360旗下多款浏览器开通“360AI大会员”，即可解锁全部应用。\n");
            Console.WriteLine(info);
        }

        Console.WriteLine("end");
    }
}

