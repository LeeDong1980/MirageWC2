using System;
using System.Numerics;
using Nethereum.Hex.HexTypes;
using MirageSDK.Core.Infrastructure;
using MirageSDK.Data;
using MirageSDK.Data.ContractMessages.ERC721;
using MirageSDK.Provider;
using MirageSDK.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MirageSDK.Core;
using MirageSDK.Mobile;
using WalletConnectSharp.Sign.Models;
using System.Collections.Generic;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Network.Models;
using WalletConnect;

public class ContractController : MonoBehaviour
{
    [SerializeField] private ContractInformationSO ERC721ContractInformation;
    [SerializeField] private ContractInformationSO NanioContractInformation;
    [SerializeField] private ProviderInformationSO ProviderInformation;

    public string WalletAddress;

    private const string APPROVE_METHOD_NAME = "approve";
    private const string PLACE_METHOD_NAME = "placeNFT";
    private const string PICKUP_METHOD_NAME = "pickupNFT";

    private IMirageSDK wrapper;
    private IContract contract;
    private IEthHandler eth;

    private IContractEventSubscriber eventSubscriber;
    private IContractEventSubscription NFTPlacedSubscription;

    public Action<NFTPlacedDTO> NFTPlacedAction;

    public WCSignClient _wc;
    public (SessionStruct, string, string) walletData;

    public void Init(WCSignClient _wc, (SessionStruct, string, string) walletData)
    {
        Debug.Log("ContractController Init");
        this._wc = _wc;
        this.walletData = walletData;
        WalletAddress = walletData.Item2;

        Debug.Log($"ContractController wcSession: {_wc.Session == null}");

        wrapper = MirageSDKFactory.GetMirageSDKInstance(ProviderInformation.HttpProviderURL, _wc);
        contract = wrapper.GetContract(NanioContractInformation.ContractAddress, NanioContractInformation.ABI);
        eth = wrapper.Eth;

        //註冊監聽 Nanio 合約上 NFTPlaced 事件
        eventSubscriber = wrapper.CreateSubscriber(ProviderInformation.WsProviderURL);
        eventSubscriber.ListenForEvents().Forget();
        eventSubscriber.OnOpenHandler += UniTask.Action(SubscribeNFTPlaced);
    }


    //public async void SendSignRequest()
    //{
    //    var (session, address, chainId) = walletData;
    //    if (string.IsNullOrWhiteSpace(address))
    //        return;

    //    var request = new EthSendTransaction(new Transaction()
    //    {
    //        From = address,
    //        To = address,
    //        Value = "0"
    //    });

    //    var result = await _wc.Request<EthSendTransaction, string>(session.Topic, request, chainId);

    //    Debug.Log("Got result from request: " + result);
    //}

    public async UniTask<NFTApprovalDTO> CallApproveNFT(string NFTAddress, string tokenId, string ownerAddress)
    {
        Debug.Log($"CallApproveNFT: Addr: {NFTAddress}, Id: {tokenId}, Owner: {ownerAddress}");

        //取得該 NFT 合約
        IContract _targetNFTContract = null;
        try
        {
            _targetNFTContract = wrapper.GetContract(NFTAddress, ERC721ContractInformation.ABI);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Cannot get NFT contract when approve, Error Message: [{e}]");
            throw;
        }

        //註冊監聽所選 NFT 合約上 Approval 事件
        EventAwaiter<NFTApprovalDTO> eventAwaiter = new EventAwaiter<NFTApprovalDTO>(NFTAddress, ProviderInformation.WsProviderURL);
        EventFilterRequest<NFTApprovalDTO> filterRequest = new EventFilterRequest<NFTApprovalDTO>();
        filterRequest.AddTopic("Owner", ownerAddress); //過濾 Approve NFT 的持有人是自己
        filterRequest.AddTopic("Approved", NanioContractInformation.ContractAddress); //過濾 Approve NFT 的對象是 Nanio 合約 
        BigInteger parseTokenID = new HexBigInteger(tokenId).Value;
        filterRequest.AddTopic("TokenId", parseTokenID); //過濾 Approve NFT 的 TokenID 是呼叫的 NFT TokenID
        await eventAwaiter.StartWaiting(filterRequest); //開始監聽

        //發送 Approve 指令
        try
        {
            string transactionHash = await _targetNFTContract.CallMethod(APPROVE_METHOD_NAME, new object[] { NanioContractInformation.ContractAddress, tokenId });
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Error occur when approve, Error Message: [{e}]");
            throw;
        }

        NFTApprovalDTO eventDto = await eventAwaiter.ReceiveEventTask; //接收到合約 Approval 事件資料
        Debug.Log($"CallApproveNFT DTO: {eventDto}");
        return eventDto;
    }

    public async UniTask CallPlaceNFT(string NFTAddress, string tokenId, int placeMinutes)
    {
        Debug.Log($"CallPlaceNFT: Addr: {NFTAddress}, Id: {tokenId}, min: {placeMinutes}");

        //發送 PlaceNFT 指令
        uint minuteParse = uint.Parse(placeMinutes.ToString());
        try
        {
            string transactionHash = await contract.CallMethod(PLACE_METHOD_NAME, new object[] { NFTAddress, tokenId, minuteParse });
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Error occur when placeNFT, Error Message: [{e}]");
            throw;
        }
    }

    public async UniTask<NFTPickupDTO> CallPickupNFT(string placementID, string ownerAddress, string NFTAddress, string tokenId)
    {
        Debug.Log($"CallPickupNFT: placementID: {placementID}, NFTAddress: {NFTAddress}");

        //取得該 NFT 合約
        IContract _targetNFTContract = null;
        try
        {
            _targetNFTContract = wrapper.GetContract(NFTAddress, ERC721ContractInformation.ABI);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Cannot get NFT contract when approve, Error Message: [{e}]");
            throw;
        }

        //註冊監聽所選 NFT 合約上 Transfer 事件
        EventAwaiter<NFTPickupDTO> eventAwaiter = new EventAwaiter<NFTPickupDTO>(NFTAddress, ProviderInformation.WsProviderURL);
        EventFilterRequest<NFTPickupDTO> filterRequest = new EventFilterRequest<NFTPickupDTO>();
        filterRequest.AddTopic("From", NanioContractInformation.ContractAddress); //過濾 Transfer 來源為 Nanio 合約
        filterRequest.AddTopic("To", ownerAddress); //過濾 Transfer NFT 的對象是自己
        BigInteger parseTokenID = new HexBigInteger(tokenId).Value;
        filterRequest.AddTopic("TokenId", parseTokenID); //過濾 Transfer NFT 的 TokenID 是撿取的 NFT TokenID
        await eventAwaiter.StartWaiting(filterRequest); //開始監聽

        //發送 PickupNFT 指令
        uint placementIDParse = uint.Parse(placementID.ToString());
        try
        {
            string transactionHash = await contract.CallMethod(PICKUP_METHOD_NAME, new object[] { placementIDParse });
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Error occur when pickupNFT, Error Message: [{e}]");
            throw;
        }

        NFTPickupDTO eventDto = await eventAwaiter.ReceiveEventTask; //接收到合約 Transfer 事件資料
        Debug.Log($"CallPickupNFT DTO: {eventDto}");
        return eventDto;
    }

    public async UniTask<bool> CheckNFTApproved(string NFTAddress, string tokenId)
    {
        var wrapper = MirageSDKFactory.GetMirageSDKInstance(ProviderInformation.HttpProviderURL);

        //取得投放 NFT 的合約
        IContract targetNFTContract = null;
        try
        {
            targetNFTContract = wrapper.GetContract(NFTAddress, ERC721ContractInformation.ABI);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Cannot get NFT contract, Error Message: {e}");
            throw;
        }

        //發送請求讀取該 NFT 的授權轉移地址
        string getApprovedAddress = "";
        HexBigInteger hexBig = new HexBigInteger(tokenId);
        BigInteger parseTokenID = hexBig.Value;
        GetApprovedMessage getApprovedOfMessage = new GetApprovedMessage { TokenID = parseTokenID };
        try
        {
            getApprovedAddress = await targetNFTContract.GetData<GetApprovedMessage, string>(getApprovedOfMessage);
            Debug.Log($"[{NFTAddress}][{tokenId}] approved to address: " + getApprovedAddress);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContractController]: Cannot get NFT approved info, Error Message: {e}");
            throw;
        }

        //比對該 NFT 授權轉移地址與 Nanio 合約地址並回傳 bool
        if (getApprovedAddress.ToLower() == NanioContractInformation.ContractAddress.ToLower())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private async UniTaskVoid SubscribeNFTPlaced()
    {
        var filters = new EventFilterData();

        NFTPlacedSubscription = await eventSubscriber.Subscribe(
            filters,
            NanioContractInformation.ContractAddress,
            (NFTPlacedDTO t) => ReceiveNFTPlacedEvent(t)
        );
    }

    private void UnsubscribeNFTPlaced()
    {
        eventSubscriber.Unsubscribe(NFTPlacedSubscription.SubscriptionId).Forget();
    }

    private void ReceiveNFTPlacedEvent(NFTPlacedDTO dto)
    {
        NFTPlacedAction?.Invoke(dto);
    }
}
