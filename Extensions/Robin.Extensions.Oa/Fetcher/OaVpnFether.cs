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

    public static Uri CalculateVpnPath(string origUriString)
    {
        var origUri = new Uri(origUriString);
        return new Uri($"/{origUri.Scheme}-{origUri.Port}/{Encrypt(origUri.Host)}{origUri.PathAndQuery}", UriKind.Relative);
    }
}

internal class OaVpnFetcher : OaFetcher
{
    private static readonly Uri _vpnUri = new Uri("https://vpn.jlu.edu.cn/");
    private static readonly Uri _vpnLoginUri = new Uri(_vpnUri, "/login?cas_login=true");
    private static readonly Uri _vpnCasUri = new Uri(_vpnUri, WebVpnHelper.CalculateVpnPath($"https://cas.jlu.edu.cn/tpass/login?service={_vpnLoginUri}"));
    private const string TicketCookieName = "wengine_vpn_ticketvpn_jlu_edu_cn";

    private readonly string _username;
    private readonly string _password;

    public OaVpnFetcher(string username, string password)
        : base(new Uri(_vpnUri, WebVpnHelper.CalculateVpnPath(_oaUri.ToString())))
    {
        _username = username;
        _password = password;
    }

    private async Task<string> Authenticate(CancellationToken token)
    {
        using var resp = await _client.GetAsync(_vpnLoginUri, token);
        var ticket = _cookies.GetCookies(_vpnUri).First(c => c.Name is TicketCookieName).Value;

        if (resp.Headers.Location == new Uri("/", UriKind.Relative))
            return ticket;

        await using var stream = await _client.GetStreamAsync(resp.Headers.Location, token);
        using var document = await _parser.ParseDocumentAsync(stream, token);

        var lt = document.QuerySelector("#lt")!.GetAttribute("value");
        var rsa = OaDes.StrEnc(_username + _password + lt);

        using var _ = await _client.PostAsync(_vpnCasUri, new FormUrlEncodedContent([
            KeyValuePair.Create("rsa", rsa),
            KeyValuePair.Create("ul", _username.Length.ToString()),
            KeyValuePair.Create("pl", _password.Length.ToString()),
            KeyValuePair.Create("sl", "0"),
            KeyValuePair.Create("lt", lt),
            KeyValuePair.Create("execution", "e1s1"),
            KeyValuePair.Create("_eventId", "submit"),
        ]));

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
