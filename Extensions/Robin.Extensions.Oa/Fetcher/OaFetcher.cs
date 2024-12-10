using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Robin.Extensions.Oa.Entity;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Robin.Extensions.Oa.Fetcher;

internal partial class OaFetcher
{
    protected static readonly Uri _oaUri = new("https://oa.jlu.edu.cn/");
    protected static readonly HtmlParser _parser = new();
    protected readonly CookieContainer _cookies = new();
    protected readonly HttpClient _client;

    public OaFetcher() : this(_oaUri) { }

    public OaFetcher(Uri baseAddress)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false,
            CookieContainer = _cookies
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            DefaultRequestHeaders = { { "User-Agent", "Robin.Extensions.Oa" } }
        };
    }

    private List<(bool Pinned, int Id)> GetPostsFromDocument(IHtmlDocument document) =>
        document.QuerySelectorAll("#itemContainer > div > a.font14")
            .Select(e => (
                e.TextContent.StartsWith("[置顶]"),
                Convert.ToInt32(HttpUtility.ParseQueryString(new Uri(_client.BaseAddress!, e.GetAttribute("href")!).Query)["id"]!)
            ))
            .ToList();

    private static Uri ResolveAttachmentUri(int postId, string attachmentId, string attachmentTitle)
    {
        static string B64Encode(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        var key = $"{attachmentId}@{attachmentTitle}@{postId}";
        var resId = B64Encode(B64Encode(B64Encode(key) + "whir"));
        return new Uri($"/defaultroot/rd/download/attachdownload.jsp?res={resId}", UriKind.Relative);
    }

    [GeneratedRegex(@"<!--.*?-->")]
    private static partial Regex CommentTagRegex { get; }

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex { get; }

    private static string ExtractText(IElement? elem)
    {
        string str = elem?.InnerHtml.Replace("<br>", "\n").Replace("<p", "\n<p").Replace("<div>", "\n<div>") ?? string.Empty;
        str = CommentTagRegex.Replace(str, string.Empty);
        str = TagRegex.Replace(str, string.Empty);
        return HttpUtility.HtmlDecode(str).Trim();
    }

    private OaPost GetPostFromDocument(IHtmlDocument document, int postId)
    {
        var title = document.QuerySelector(".content > .content_t")!.TextContent;
        var dateTime = DateTime.Parse(document.QuerySelector(".content > .content_time")!.FirstChild!.TextContent);
        var source = document.QuerySelector(".content > .content_time > span")!.TextContent;
        var content = ExtractText(document.QuerySelector(".content > .content_font"));
        var images = document.QuerySelectorAll(".content img")
            .Where(e => !e.GetAttribute("src")!.StartsWith("data"))
            .Select(e => new Uri(_client.BaseAddress!, e.GetAttribute("src")!)).ToList();
        var dataImages = document.QuerySelectorAll(".content img")
            .Where(e => e.GetAttribute("src")!.StartsWith("data"))
            .Select(e => e.GetAttribute("src")!).ToList();
        var attachments = document.QuerySelectorAll(".content > .news_aboutFile span").Select(e => new OaAttachment
        {
            Name = e.GetAttribute("title")!,
            Url = new Uri(_client.BaseAddress!, ResolveAttachmentUri(postId, e.Id!, e.GetAttribute("title")!))
        }).ToList();

        return new OaPost
        {
            Id = postId,
            Title = title,
            DateTime = dateTime,
            Source = source,
            Content = content,
            Images = images,
            DataImages = dataImages,
            Attachments = attachments
        };
    }

    public async virtual Task<Stream> FetchBlobAsync(Uri uri, CancellationToken token) =>
        await _client.GetStreamAsync(uri, token);

    public async virtual Task<IEnumerable<(bool Pinned, int Id)>> FetchPostsAsync(CancellationToken token)
    {
        await using var stream = await _client.GetStreamAsync("/defaultroot/PortalInformation!jldxList.action?channelId=179577", token);
        using var document = await _parser.ParseDocumentAsync(stream, token);
        return GetPostsFromDocument(document);
    }

    public async virtual Task<OaPost> FetchPostAsync(int postId, CancellationToken token)
    {
        await using var stream = await _client.GetStreamAsync($"/defaultroot/PortalInformation!getInformation.action?id={postId}&channelId=179577", token);
        using var document = await _parser.ParseDocumentAsync(stream, token);
        return GetPostFromDocument(document, postId);
    }
}
