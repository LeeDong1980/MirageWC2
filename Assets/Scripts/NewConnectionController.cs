using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityBinder;
using WalletConnect;
using System;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models;
using WalletConnectUnity.Demo.SimpleSign;
using WalletConnectUnity.Demo.Utils;
using UnityEngine.UI;
using System.Linq;
using MirageSDK.WalletConnect.VersionShared.Models.Ethereum.Types;

public class NewConnectionController : BindableMonoBehavior
{
    [Inject]
    private WCSignClient WC;
    [SerializeField] ContractController contractController;
    [SerializeField] GameObject loginPage;
    [SerializeField] GameObject connectedPage;
    [SerializeField] Button loginBtn;
    [SerializeField] Button logoutBtn;

    private void Start()
    {
        loginBtn.onClick.AddListener(OnConnect);
        logoutBtn.onClick.AddListener(SignOut);
    }

    public async void OnConnect()
    {
        Chain goerliChain = Chain.EthereumGoerli;

        if (WC == null)
        {
            Debug.LogError("No WCSignClient scripts found in scene!");
            return;
        }

        // Connect Sign Client
        Debug.Log("Connecting sign client..");
        ProposedNamespace proposedNamespace = new()
        {
            Chains = new string[] { goerliChain.FullChainId },
            Events = new string[] { "chainChanged", "accountsChanged" },
            Methods = new string[]
            {
                "eth_sendTransaction",
                "eth_signTransaction",
                "eth_sign",
                "personal_sign",
                "eth_signTypedData",
                "eth_getBalance",
            }
        };
        var requiriedNamespaces = new RequiredNamespaces { { Chain.EvmNamespace, proposedNamespace } };

        var dappConnectOptions = new ConnectOptions()
        {
            RequiredNamespaces = requiriedNamespaces
        };

        var connectData = await WC.Connect(dappConnectOptions);

        Debug.Log($"Connection successful, URI: {connectData.Uri}");

        try
        {
            await connectData.Approval;

            // We need to move this to the main unity thread
            // TODO Perhaps ensure we are using Unity's Sync context inside WalletConnectSharp
            MTQ.Enqueue(() =>
            {
                Debug.Log($"Connection approved, URI: {connectData.Uri}");

                // Enable auth example canvas and disable outselves
                //gameObject.SetActive(false);
                //AuthScreen.SetActive(true);
                loginPage.SetActive(false);
                connectedPage.SetActive(true);
                var walletData = GetCurrentAddress();
                contractController.Init(WC, walletData);
            });
        }
        catch (Exception e)
        {
            Debug.LogError(("Connection failed: " + e.Message));
            Debug.LogError(e);
        }
    }

    public async void SignOut()
    {
        await WC.Disconnect(WC.Session.Values[0].Topic);

        // TODO Perhaps ensure we are using Unity's Sync context inside WalletConnectSharp
        MTQ.Enqueue(() =>
        {
            loginPage.SetActive(true);
            connectedPage.SetActive(false);
            contractController.WalletAddress = "";
        });
    }

    private (SessionStruct, string, string) GetCurrentAddress()
    {
        var currentSession = WC.Session.Get(WC.Session.Keys[0]);

        var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(defaultChain))
            return (default, null, null);

        var defaultNamespace = currentSession.Namespaces[defaultChain];

        if (defaultNamespace.Accounts.Length == 0)
            return (default, null, null);

        var fullAddress = defaultNamespace.Accounts[0];
        var addressParts = fullAddress.Split(":");

        var address = addressParts[2];
        var chainId = string.Join(':', addressParts.Take(2));

        return (currentSession, address, chainId);
    }
}
