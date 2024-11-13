namespace Robin.Extensions.Oa;

internal record OaConsumerData(int LastPinnedPostId, int LastNormalPostId);

internal record OaData(Dictionary<long, OaConsumerData> Groups);
