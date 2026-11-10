using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Guards;

[TestClass]
public class BuildConfigurationTests
{
    private static readonly string Root = RepoRoot.Find();

    private static readonly string[] V2Projects =
    [
        "src/k-spider-admin/k-spider-admin.csproj",
        "src/k-spider-crawler/k-spider-crawler.csproj",
        "src/k-spider-process/k-spider-process.csproj",
        "src/k-spider-grpc/k-spider-grpc.csproj",
    ];

    [TestMethod]
    public void V2ProjectsUseCentralPackageManagementWithoutVersions()
    {
        foreach (var project in V2Projects)
        {
            var document = XDocument.Load(Path.Combine(Root, project));
            var references = document.Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .ToArray();

            foreach (var reference in references)
            {
                Assert.IsNull(reference.Attribute("Version"), $"{project} 的 PackageReference 不应写版本");
                Assert.IsFalse(
                    reference.Elements().Any(element => element.Name.LocalName == "Version"),
                    $"{project} 的 PackageReference 不应写版本");
            }
        }
    }

    [TestMethod]
    public void BuildPropsEnableQualityGates()
    {
        var document = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));
        var values = document.Descendants()
            .Where(element => element.Name.LocalName is "TreatWarningsAsErrors" or "AnalysisLevel" or "EnforceCodeStyleInBuild")
            .ToDictionary(element => element.Name.LocalName, element => element.Value.Trim(), StringComparer.Ordinal);

        Assert.IsTrue(
            values.TryGetValue("TreatWarningsAsErrors", out var treatWarningsAsErrors) &&
            string.Equals(treatWarningsAsErrors, "true", StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props 未开启 TreatWarningsAsErrors");
        Assert.IsTrue(
            values.TryGetValue("EnforceCodeStyleInBuild", out var enforceCodeStyle) &&
            string.Equals(enforceCodeStyle, "true", StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props 未开启 EnforceCodeStyleInBuild");
        Assert.IsTrue(
            values.TryGetValue("AnalysisLevel", out var analysisLevel) &&
            string.Equals(analysisLevel, "latest-recommended", StringComparison.OrdinalIgnoreCase),
            "Directory.Build.props 未设置 AnalysisLevel=latest-recommended");
    }

    [TestMethod]
    public void NuGetConfigPinsSingleSource()
    {
        var path = Path.Combine(Root, "nuget.config");
        Assert.IsTrue(File.Exists(path), "缺少 nuget.config");
        var document = XDocument.Load(path);
        Assert.IsTrue(
            document.Descendants().Any(element => element.Name.LocalName == "clear"),
            "nuget.config 必须先用 <clear /> 清空机器级源");
        Assert.IsTrue(
            document.Descendants()
                .Where(element => element.Name.LocalName == "add")
                .Select(element => element.Attribute("value")?.Value)
                .Any(value => string.Equals(value, "https://api.nuget.org/v3/index.json", StringComparison.Ordinal)),
            "nuget.config 必须显式声明 nuget.org");
    }

    [TestMethod]
    public void CentralPackageTransitivePinningIsEnabled()
    {
        var document = XDocument.Load(Path.Combine(Root, "Directory.Packages.props"));
        Assert.IsTrue(
            document.Descendants()
                .Where(element => element.Name.LocalName == "CentralPackageTransitivePinningEnabled")
                .Any(element => string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase)),
            "Directory.Packages.props 未开启传递依赖钉版");
    }

    [TestMethod]
    public void VerifyScriptRunsV2GuardTests()
    {
        var script = File.ReadAllText(Path.Combine(Root, "scripts", "verify.sh"));
        Assert.IsTrue(
            script.Contains("dotnet test \"$SOLUTION\"", StringComparison.Ordinal),
            "verify.sh 未运行解决方案中的 v2 守卫测试");
    }
}
