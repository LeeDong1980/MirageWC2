using MirageSDK.Data;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

[Event("Approval")]
public class NFTApprovalDTO : EventDTOBase
{
    [Parameter("address", "owner", 1, true)]
    public string Owner { get; set; }

    [Parameter("address", "approved", 2, true)]
    public string Approved { get; set; }

    [Parameter("uint256", "tokenId", 3, true)]
    public BigInteger TokenId { get; set; }
}
