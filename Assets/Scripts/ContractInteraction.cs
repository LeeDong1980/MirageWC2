using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContractInteraction : MonoBehaviour
{
    [SerializeField] ContractController contractController;

    [Header("Approve NFT to Nanio")]
    [SerializeField] TMP_InputField approveNFTAddr;
    [SerializeField] TMP_InputField approveTokenID;
    [SerializeField] Button approveBtn;

    [Header("Place NFT to Nanio")]
    [SerializeField] TMP_InputField placeNFTAddr;
    [SerializeField] TMP_InputField placeTokenID;
    [SerializeField] Button placeBtn;

    [Header("Pickup NFT from Nanio")]
    [SerializeField] TMP_InputField pickupPid;
    [SerializeField] TMP_InputField pickupNFTAddr;
    [SerializeField] TMP_InputField pickupTokenID;
    [SerializeField] Button pickupBtn;

    private void Start()
    {
        approveBtn.onClick.AddListener(ApproveNFTToNanio);
        placeBtn.onClick.AddListener(PlaceNFT);
        pickupBtn.onClick.AddListener(PickupNFT);
    }

    public async void ApproveNFTToNanio()
    {
        string nftAddr = approveNFTAddr.text;
        string tokenId = approveTokenID.text;
        await contractController.CallApproveNFT(nftAddr, tokenId, contractController.walletData.Item2);
    }

    public async void PlaceNFT()
    {
        string nftAddr = placeNFTAddr.text;
        string tokenId = placeTokenID.text;
        await contractController.CallPlaceNFT(nftAddr, tokenId, 0);
    }

    public async void PickupNFT()
    {
        string pid = pickupPid.text;
        string nftAddr = pickupNFTAddr.text;
        string tokenId = pickupTokenID.text;
        
        await contractController.CallPickupNFT(pid, contractController.walletData.Item2, nftAddr, tokenId);
    }
}
