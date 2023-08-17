using MirageSDK.Data;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

[Event("NFTPlaced")]
public class NFTPlacedDTO : EventDTOBase
{
    [Parameter("address", "NFTAddr", 1, false)]
    public string NFTAddress { get; set; }
    [Parameter("uint256", "tokenId", 2, false)]
    public BigInteger TokenId { get; set; }
    [Parameter("uint256", "startTime", 3, false)]
    public BigInteger StartTime { get; set; }
    [Parameter("uint256", "endTime", 4, false)]
    public BigInteger EndTime { get; set; }
    [Parameter("uint256", "placementID", 5, false)]
    public BigInteger PlacementID { get; set; }
    [Parameter("address", "currentNFTOwner", 6, false)]
    public string CurrentNFTOwner { get; set; }
}