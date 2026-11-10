using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Guards;

[TestClass]
public class SolutionLayoutTests
{
    private static readonly string Root = RepoRoot.Find();

    private static readonly string[] ExpectedProjects =
    [
        "src/k-spider-admin/k-spider-admin.csproj",
        "src/k-spider-crawler/k-spider-crawler.csproj",
        "src/k-spider-process/k-spider-process.csproj",
        "src/k-spider-grpc/k-spider-grpc.csproj",
        "tests/k-spider-guards/k-spider-guards.csproj",
        "tests/k-spider-grpc-tests/k-spider-grpc-tests.csproj",
        "tests/k-spider-admin-tests/k-spider-admin-tests.csproj",
    ];

    private static readonly string[] V2Projects =
    [
        "src/k-spider-admin/k-spider-admin.csproj",
        "src/k-spider-crawler/k-spider-crawler.csproj",
        "src/k-spider-process/k-spider-process.csproj",
    ];

    private static readonly string[] GrpcConsumers =
    [
        "src/k-spider-admin/k-spider-admin.csproj",
        "src/k-spider-crawler/k-spider-crawler.csproj",
    ];

    [TestMethod]
    public void SolutionContainsV2ProjectsAndGuardProject()
    {
        var solution = ReadSolution();
        foreach (var project in ExpectedProjects)
        {
            Assert.IsTrue(
                solution.Contains(project, StringComparison.OrdinalIgnoreCase),
                $"解决方案缺少项目：{project}");
        }
    }

    [TestMethod]
    public void SolutionDoesNotIncludeV1ArchiveOrTransitionTests()
    {
        var solution = ReadSolution();
        Assert.IsFalse(
            solution.Contains("k-spider-dotnet-old", StringComparison.OrdinalIgnoreCase),
            "v1 归档项目不应加入解决方案");
        Assert.IsFalse(
            solution.Contains("k-spider-test", StringComparison.OrdinalIgnoreCase),
            "v1 过渡测试不应加入解决方案");
    }

    [TestMethod]
    public void V2ProjectsDoNotReferenceEachOtherOrV1Archive()
    {
        foreach (var project in V2Projects)
        {
            var document = XDocument.Load(Path.Combine(Root, project));
            var references = document.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            Assert.IsFalse(
                references.Any(reference => reference.Contains("k-spider-dotnet-old", StringComparison.OrdinalIgnoreCase)),
                $"{project} 不应引用 v1 归档项目");

            foreach (var other in V2Projects.Where(other => !string.Equals(other, project, StringComparison.Ordinal)))
            {
                var otherName = Path.GetFileNameWithoutExtension(other);
                Assert.IsFalse(
                    references.Any(reference => reference.Contains(otherName, StringComparison.OrdinalIgnoreCase)),
                    $"{project} 不应直接引用服务项目 {otherName}；跨服务契约走 src/k-spider-grpc/");
            }
        }
    }

    [TestMethod]
    public void V2ServicesReferenceGrpcContract()
    {
        foreach (var project in GrpcConsumers)
        {
            var document = XDocument.Load(Path.Combine(Root, project));
            var references = document.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .ToArray();

            Assert.IsTrue(
                references.Any(reference => reference.Contains("k-spider-grpc", StringComparison.OrdinalIgnoreCase)),
                $"{project} 应引用 k-spider-grpc 契约项目");
        }
    }

    [TestMethod]
    public void DevAndStopScriptsExist()
    {
        Assert.IsTrue(File.Exists(Path.Combine(Root, "scripts", "dev.sh")), "缺少 scripts/dev.sh");
        Assert.IsTrue(File.Exists(Path.Combine(Root, "scripts", "stop.sh")), "缺少 scripts/stop.sh");
    }

    private static string ReadSolution()
    {
        var solution = File.ReadAllText(Path.Combine(Root, "k-spider-dotnet.sln"));
        return solution.Replace('\\', '/');
    }
}
