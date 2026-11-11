using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Guards;

/// <summary>
///     守卫注释一致性检查本身不被静默移除：
///     后端 XML 文档生成负责校验 param/returns 与签名，前端 ESLint jsdoc 负责标签与签名。
/// </summary>
[TestClass]
public sealed class CommentConsistencyTests
{
    private static readonly string Root = RepoRoot.Find();

    [TestMethod]
    public void BackendGeneratesXmlDocumentationWithoutMissingDocNoise()
    {
        var document = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));

        var generateDocumentation = document.Descendants()
            .Where(element => element.Name.LocalName == "GenerateDocumentationFile")
            .Select(element => element.Value.Trim())
            .ToArray();
        Assert.IsTrue(
            generateDocumentation.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)),
            "Directory.Build.props 未开启 GenerateDocumentationFile；XML 注释不会参与编译检查");

        var noWarn = document.Descendants()
            .Where(element => element.Name.LocalName == "NoWarn")
            .ToArray();
        Assert.IsTrue(
            noWarn.Any(element =>
                element.Value.Contains("CS1591", StringComparison.Ordinal) &&
                element.Parent?.Attribute("Condition") is not null),
            "Directory.Build.props 未对生成/测试项目豁免 CS1591");
        Assert.IsFalse(
            noWarn.Any(element =>
                element.Value.Contains("CS1591", StringComparison.Ordinal) &&
                element.Parent?.Attribute("Condition") is null),
            "CS1591 不得全局豁免；否则业务项目缺失 XML 文档不会红灯");
    }

    [TestMethod]
    public void FrontendEnablesJsdocSignatureRules()
    {
        var config = File.ReadAllText(Path.Combine(Root, "web", "eslint.config.js"));
        foreach (var rule in new[]
                 {
                     "eslint-plugin-jsdoc",
                     "jsdoc/require-jsdoc",
                     "jsdoc/check-param-names",
                     "jsdoc/check-types",
                     "jsdoc/require-param",
                     "jsdoc/require-returns",
                     "jsdoc/require-returns-check",
                 })
        {
            Assert.IsTrue(config.Contains(rule, StringComparison.Ordinal), $"eslint.config.js 缺少 {rule}");
        }

        var packageJson = File.ReadAllText(Path.Combine(Root, "web", "package.json"));
        Assert.IsTrue(
            packageJson.Contains("\"eslint-plugin-jsdoc\"", StringComparison.Ordinal),
            "web/package.json 缺少 eslint-plugin-jsdoc devDependency");

        var verifyScript = File.ReadAllText(Path.Combine(Root, "scripts", "verify.sh"));
        Assert.IsTrue(
            verifyScript.Contains("pnpm run lint", StringComparison.Ordinal),
            "verify.sh 未执行前端 lint；注释一致性检查不会进入门禁");
        Assert.IsTrue(
            verifyScript.Contains("dotnet build", StringComparison.Ordinal),
            "verify.sh 未执行后端构建；XML 注释一致性检查不会进入门禁");
    }
}
