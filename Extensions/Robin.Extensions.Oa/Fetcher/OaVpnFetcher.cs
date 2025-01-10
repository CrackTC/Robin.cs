using System.Net;
using System.Security.Cryptography;
using System.Text;
using Robin.Extensions.Oa.Entity;

namespace Robin.Extensions.Oa.Fetcher;

file static class WebVpnHelper
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("Didida1127Didida");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("Didida1127Didida");
    private static readonly Aes aes = Aes.Create();

    static WebVpnHelper()
    {
        aes.Key = Key;
    }

    private static string Encrypt(string domain)
    {
        var bytes = Encoding.UTF8.GetBytes(domain);
        return Convert.ToHexString(IV) +
            Convert.ToHexString(aes.EncryptCfb(bytes, IV, PaddingMode.Zeros, feedbackSizeInBits: 128).AsSpan(0, bytes.Length));
    }

    private static string GetPath1(Uri uri) => uri switch
    {
        { Scheme: "http", Port: 80 } => "http",
        { Scheme: "https", Port: 443 } => "https",
        _ => $"{uri.Scheme}-{uri.Port}"
    };

    public static Uri CalculateVpnPath(string origUriString)
    {
        var origUri = new Uri(origUriString);
        return new Uri($"/{GetPath1(origUri)}/{Encrypt(origUri.Host).ToLower()}{origUri.PathAndQuery}", UriKind.Relative);
    }
}

internal class OaVpnFetcher(string username, string password)
    : OaFetcher(new Uri(_vpnUri, WebVpnHelper.CalculateVpnPath(_oaUri.ToString())))
{
    private static readonly Uri _vpnUri = new("https://vpn.jlu.edu.cn/");
    private static readonly Uri _vpnLoginUri = new(_vpnUri, "/login?cas_login=true");
    private static readonly Uri _vpnCasUri = new(
        _vpnUri,
        WebVpnHelper.CalculateVpnPath($"https://cas.jlu.edu.cn/tpass/login?service={WebUtility.UrlEncode(_vpnLoginUri.ToString())}")
    );
    private const string TicketCookieName = "wengine_vpn_ticketvpn_jlu_edu_cn";

    private readonly string _username = username;
    private readonly string _password = password;

    private async Task<string> Authenticate(CancellationToken token)
    {
        using var resp = await _client.GetAsync(_vpnLoginUri, token);
        var ticket = _cookies.GetCookies(_vpnUri).First(c => c.Name is TicketCookieName).Value;

        // already authenticated
        if (resp.Headers.Location == new Uri("/", UriKind.Relative))
            return ticket;

        await using var stream = await _client.GetStreamAsync(resp.Headers.Location, token);
        using var document = await _parser.ParseDocumentAsync(stream, token);

        var lt = document.QuerySelector("#lt")!.GetAttribute("value");
        var execution = document.QuerySelector("input[name=execution]")!.GetAttribute("value");
        var rsa = OaDes.StrEnc(_username + _password + lt);

        using var loginResp = await _client.PostAsync(_vpnCasUri, new FormUrlEncodedContent([
            KeyValuePair.Create("rsa", rsa),
            KeyValuePair.Create("ul", _username.Length.ToString()),
            KeyValuePair.Create("pl", _password.Length.ToString()),
            KeyValuePair.Create("sl", "0"),
            KeyValuePair.Create("lt", lt),
            KeyValuePair.Create("execution", execution),
            KeyValuePair.Create("_eventId", "submit"),
        ]), token);

        using var stResp = await _client.GetAsync(loginResp.Headers.Location, token);
        using var tokenResp = await _client.GetAsync(stResp.Headers.Location, token);
        using var tokenResp1 = await _client.GetAsync(tokenResp.Headers.Location, token);

        return ticket;
    }

    public async override Task<Stream> FetchBlobAsync(Uri uri, CancellationToken token)
    {
        await Authenticate(token);
        return await base.FetchBlobAsync(uri, token);
    }

    public async override Task<IEnumerable<(bool Pinned, int Id)>> FetchPostsAsync(CancellationToken token)
    {
        await Authenticate(token);
        return await base.FetchPostsAsync(token);
    }

    public async override Task<OaPost> FetchPostAsync(int postId, CancellationToken token)
    {
        await Authenticate(token);
        return await base.FetchPostAsync(postId, token);
    }
}
