using JianyingVideoAssistant.Infrastructure;

var root = Path.Combine(Path.GetTempPath(), $"jva-install-test-{Guid.NewGuid():N}");
var server = LocalWorkbenchInstallation.ServerDirectory(root);
Directory.CreateDirectory(Path.Combine(server, "static-workbench"));
File.WriteAllText(Path.Combine(server, "JianyingVideoAssistant.Server.exe"), "test");
File.WriteAllText(Path.Combine(server, "static-workbench", "index.html"), "test");

LocalWorkbenchInstallation.Validate(root);
if (!LocalWorkbenchInstallation.DataRoot.EndsWith(Path.Combine("JianyingVideoAssistant", "Data"), StringComparison.Ordinal))
    throw new InvalidOperationException("本地数据目录未隔离到应用目录。");

Directory.Delete(root, recursive: true);
Console.WriteLine("独立本地工作台安装结构测试通过。");
