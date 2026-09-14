namespace JianyingVideoAssistant.Infrastructure;

public static class WorkbenchEndpoint
{
    public const string DefaultUrl = "http://192.168.31.24:8000/workbench/";
    public const string EnvironmentVariable = "JVA_WORKBENCH_URL";
    public const string DisplayHost = "192.168.31.24 视频生产服务";

    public static Uri Resolve()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            uri = new Uri(DefaultUrl, UriKind.Absolute);
        }

        var builder = new UriBuilder(uri);
        if (!builder.Path.EndsWith('/')) builder.Path += "/";
        return builder.Uri;
    }
}
