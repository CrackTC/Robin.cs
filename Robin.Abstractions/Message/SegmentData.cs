using System.Text.Json.Serialization;
using Robin.Abstractions.Message.Entity;

namespace Robin.Abstractions.Message;

[JsonDerivedType(typeof(AnonymousData))]
[JsonDerivedType(typeof(AtData))]
[JsonDerivedType(typeof(CustomMusicData))]
[JsonDerivedType(typeof(CustomNodeData))]
[JsonDerivedType(typeof(DiceData))]
[JsonDerivedType(typeof(FaceData))]
[JsonDerivedType(typeof(ForwardData))]
[JsonDerivedType(typeof(FriendContactData))]
[JsonDerivedType(typeof(GroupContactData))]
[JsonDerivedType(typeof(ImageData))]
[JsonDerivedType(typeof(JsonData))]
[JsonDerivedType(typeof(KeyboardData))]
[JsonDerivedType(typeof(LocationData))]
[JsonDerivedType(typeof(LongMessageData))]
[JsonDerivedType(typeof(MusicData))]
[JsonDerivedType(typeof(NodeData))]
[JsonDerivedType(typeof(PokeData))]
[JsonDerivedType(typeof(RecordData))]
[JsonDerivedType(typeof(ReplyData))]
[JsonDerivedType(typeof(RpsData))]
[JsonDerivedType(typeof(ShakeData))]
[JsonDerivedType(typeof(ShareData))]
[JsonDerivedType(typeof(TextData))]
[JsonDerivedType(typeof(VideoData))]
[JsonDerivedType(typeof(XmlData))]
public abstract record SegmentData;
