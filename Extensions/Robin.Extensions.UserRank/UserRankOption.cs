namespace Robin.Extensions.UserRank;

public class UserRankOption
{
    public int TopN { get; set; }
    public required List<string> FontPaths { get; set; }
    public required Dictionary<string, string> ColorPalette { get; set; }
    public float CardWidth { get; set; }
    public float CardHeight { get; set; }
    public float CardGap { get; set; }
    public float PrimaryFontSize { get; set; }
}
