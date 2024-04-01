namespace Robin.Extensions.RandReply;

[Serializable]
public class RandReplyOption
{
    public required List<string> Texts { get; set; }
    public required List<string> ImagePaths { get; set; }
}
