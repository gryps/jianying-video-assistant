namespace JianyingVideoAssistant.Infrastructure;

public static class LocalWorkbenchInstallation
{
    public static string DataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JianyingVideoAssistant",
        "Data");

    public static string ServerDirectory(string applicationDirectory) =>
        Path.Combine(applicationDirectory, "LocalWorkbench");

    public static void Validate(string applicationDirectory)
    {
        var serverDirectory = ServerDirectory(applicationDirectory);
        var executable = Path.Combine(serverDirectory, "JianyingVideoAssistant.Server.exe");
        var index = Path.Combine(serverDirectory, "static-workbench", "index.html");
        if (!File.Exists(executable) || !File.Exists(index))
            throw new FileNotFoundException("安装包中的本地工作台组件不完整，请重新安装应用。", executable);
    }
}
