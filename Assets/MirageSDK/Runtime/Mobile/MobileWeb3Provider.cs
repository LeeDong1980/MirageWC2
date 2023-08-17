using System;
using System.Linq;
using System.Net.Http.Headers;
using Cysharp.Threading.Tasks;
using MirageSDK.Utils;
using MirageSDK.WalletConnect.VersionShared.Infrastructure;
using MirageSDK.WalletConnect.VersionShared.Models;
using MirageSDK.WalletConnectSharp.NEthereum;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using UnityBinder;
using WalletConnect;
using WalletConnectSharp.Sign.Controllers;
using WalletConnectSharp.Sign.Models;
using Newtonsoft.Json;

namespace MirageSDK.Mobile
{
	public class MobileWeb3Provider
	{
		//private readonly WalletConnectSharp.Unity.WalletConnect _walletConnect;
		[Inject]
        private WCSignClient _wc;

        public MobileWeb3Provider()
		{
			//_walletConnect = ConnectProvider<WalletConnectSharp.Unity.WalletConnect>.GetConnect();
		}

        //public IWeb3 CreateWeb3(string providerURI)
        //{
        //	var client = _walletConnect.CreateProvider(new Uri(providerURI));
        //	var web3 = new Web3(client);
        //	return web3;
        //}

        //WCv2
        public IWeb3 CreateWeb3(string providerURI)
        {
            //IClient client = GenericRequest(new Uri(providerURI));
            //var client = _walletConnect.CreateProvider(new Uri(providerURI));
            var web3 = new Web3(providerURI);
            return web3;
        }

        //public IClient CreateProvider(Uri url, AuthenticationHeaderValue authenticationHeader = null)
        //{
        //    return new RpcClient(url, authenticationHeader);
        //}

        //public async UniTask<GenericJsonRpcResponse> GenericRequest(GenericJsonRpcRequest genericRequest)
        //{
        //    //CheckIfSessionCreated();
        //    var (session, address, chainId) = GetCurrentAddress();
        //    return await _wc.Request<GenericJsonRpcRequest, GenericJsonRpcResponse>(session.Topic, genericRequest, chainId);
        //}

        //public (SessionStruct, string, string) GetCurrentAddress()
        //{
        //    var currentSession = _wc.Session.Get(_wc.Session.Keys[0]);

        //    var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();

        //    if (string.IsNullOrWhiteSpace(defaultChain))
        //        return (default, null, null);

        //    var defaultNamespace = currentSession.Namespaces[defaultChain];

        //    if (defaultNamespace.Accounts.Length == 0)
        //        return (default, null, null);

        //    var fullAddress = defaultNamespace.Accounts[0];
        //    var addressParts = fullAddress.Split(":");

        //    var address = addressParts[2];
        //    var chainId = string.Join(':', addressParts.Take(2));

        //    return (currentSession, address, chainId);
        //}
    }
}