using JianyingVideoAssistant.Infrastructure;

AssertEqual(
    "http://192.168.31.24:8000/workbench/",
    WorkbenchEndpoint.Resolve().AbsoluteUri,
    "默认连接生产工作台");

Environment.SetEnvironmentVariable(WorkbenchEndpoint.EnvironmentVariable, "https://video.example.test/workbench");
AssertEqual(
    "https://video.example.test/workbench/",
    WorkbenchEndpoint.Resolve().AbsoluteUri,
    "允许部署环境覆盖服务地址");

Environment.SetEnvironmentVariable(WorkbenchEndpoint.EnvironmentVariable, "not-a-url");
AssertEqual(
    WorkbenchEndpoint.DefaultUrl,
    WorkbenchEndpoint.Resolve().AbsoluteUri,
    "无效配置回退默认服务地址");

Console.WriteLine("桌面工作台入口配置测试通过。");

static void AssertEqual<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}失败：期望 {expected}，实际 {actual}");
}
