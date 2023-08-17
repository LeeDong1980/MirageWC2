using System;
using System.Numerics;
using MirageSDK.Core.Infrastructure;
using MirageSDK.Utils;
using MirageSDK.WalletConnect.VersionShared.Models.Ethereum;
using MirageSDK.WalletConnectSharp.Core;
using MirageSDK.WalletConnectSharp.Core.Events.Model.Ethereum;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using WalletConnect;
using System.Linq;
using UnityBinder;
using Nethereum.Signer;
using WalletConnectSharp.Sign.Controllers;
using WalletConnectSharp.Sign.Models;
using System.Diagnostics;

namespace MirageSDK.Mobile
{
	public class EthHandler : IEthHandler
	{
		private readonly IWeb3 _web3Provider;
		private readonly WalletConnectSharp.Unity.WalletConnect _walletConnect;
		private readonly ISilentSigningHandler _silentSigningHandler;


        private WCSignClient _wc;

  //      public EthHandler(IWeb3 web3Provider, ISilentSigningHandler silentSigningHandler)
		//{
		//	_web3Provider = web3Provider;
		//	_silentSigningHandler = silentSigningHandler;
		//	//_walletConnect = ConnectProvider<WalletConnectSharp.Unity.WalletConnect>.GetConnect();
		//}

		//WCv2
        public EthHandler(IWeb3 web3Provider, ISilentSigningHandler silentSigningHandler, WCSignClient _wc = null)
        {
            _web3Provider = web3Provider;
            _silentSigningHandler = silentSigningHandler;
			this._wc = _wc;
        }

        public async UniTask WalletAddEthChain(EthChainData chainData)
		{
			if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
			{ 
				throw new Exception("Application is not linked to wallet");
			}

			await _walletConnect.WalletAddEthChain(chainData);
		}

		public async UniTask WalletSwitchEthChain(EthChain chain)
		{
			if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
			{
				throw new Exception("Application is not linked to wallet");
			}

			await _walletConnect.WalletSwitchEthChain(chain);
		}

		public async UniTask WalletUpdateEthChain(EthUpdateChainData chain)
		{
			if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
			{
				throw new Exception("Application is not linked to wallet");
			}

			await  _walletConnect.WalletUpdateEthChain(chain);
		}

		public UniTask<BigInteger> EthChainId()
		{
			if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
			{
				throw new Exception("Application is not linked to wallet");
			}

			return _walletConnect.EthChainId();
		}

        //public UniTask<string> GetDefaultAccount()
        //{
        //	if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
        //	{
        //		throw new Exception("Application is not linked to wallet");
        //	}

        //	return _walletConnect.GetDefaultAccount();
        //}

		//WCv2
        public UniTask<string> GetDefaultAccount()
        {
            var currentSession = _wc.Session.Get(_wc.Session.Keys[0]);
            var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();
            var defaultNamespace = currentSession.Namespaces[defaultChain];

            var fullAddress = defaultNamespace.Accounts[0];
            var addressParts = fullAddress.Split(":");
            string address = addressParts[2];
            return UniTask.FromResult(address);
        }

        public UniTask<BigInteger> GetChainId()
		{
			if (_walletConnect.Status == WalletConnectStatus.Uninitialized)
			{
				throw new Exception("Application is not linked to wallet");
			}

			var chainId = _walletConnect.ChainId;
			return UniTask.FromResult(new BigInteger(chainId));
		}

		public UniTask<TransactionReceipt> GetTransactionReceipt(string transactionHash)
		{
			return _web3Provider.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionHash)
				.AsUniTask();
		}

		public UniTask<Transaction> GetTransaction(string transactionHash)
		{
			var transactionByHash = new EthGetTransactionByHash(_web3Provider.Client);
			return transactionByHash.SendRequestAsync(transactionHash).AsUniTask();
		}

		public UniTask<HexBigInteger> EstimateGas(
			string from,
			string to,
			string data = null,
			string value = null,
			string gas = null,
			string gasPrice = null,
			string nonce = null
		)
		{
			var transactionInput = new TransactionInput(to, from)
			{
				Gas = gas != null ? new HexBigInteger(gas) : null,
				GasPrice = gasPrice != null ? new HexBigInteger(gasPrice) : null,
				Nonce = nonce != null ? new HexBigInteger(nonce) : null,
				Value = value != null ? new HexBigInteger(value) : null,
				Data = data
			};

			return EstimateGas(transactionInput);
		}

		public UniTask<string> Sign(string messageToSign, string address)
		{
			if (_silentSigningHandler != null && _silentSigningHandler.IsSilentSigningActive())
			{
				_silentSigningHandler.SilentSignMessage(messageToSign, address);
			}

			return _walletConnect.EthSign(address, messageToSign);
		}

        //public async UniTask<string> SendTransaction(string from, string to, string data = null, string value = null,
        //	string gas = null,
        //	string gasPrice = null, string nonce = null)
        //{
        //	if (_silentSigningHandler != null && _silentSigningHandler.IsSilentSigningActive())
        //	{
        //		var hash = await _silentSigningHandler.SendSilentTransaction(from, to, data, value, gas, gasPrice,
        //			nonce);
        //		return hash;
        //	}

        //	var transactionData = new TransactionData
        //	{
        //		from = from, to = to, data = data,
        //		value = value != null ? MirageSDKHelper.StringToBigInteger(value) : null,
        //		gas = gas != null ? MirageSDKHelper.StringToBigInteger(gas) : null,
        //		gasPrice = gasPrice != null ? MirageSDKHelper.StringToBigInteger(gasPrice) : null, nonce = nonce
        //	};
        //	var request = new EthSendTransaction(transactionData);
        //	var response = await _walletConnect
        //		.Send<EthSendTransaction, EthResponse>(request);
        //	return response.Result;
        //}

		//WCv2
        public async UniTask<string> SendTransaction(string from, string to, string data = null, string value = null,
            string gas = null,
            string gasPrice = null, string nonce = null)
        {
            if (_silentSigningHandler != null && _silentSigningHandler.IsSilentSigningActive())
            {
                var hash = await _silentSigningHandler.SendSilentTransaction(from, to, data, value, gas, gasPrice,
                    nonce);
                return hash;
            }

            DemoDapp.Transaction transactionData = new DemoDapp.Transaction
            {
				From = from,
                To = to,
                Data = data,
                Value = value != null ? MirageSDKHelper.StringToBigInteger(value) : null,
                Gas = gas != null ? MirageSDKHelper.StringToBigInteger(gas) : null,
                GasPrice = gasPrice != null ? MirageSDKHelper.StringToBigInteger(gasPrice) : null,
                //Nonce = nonce
            };

            var request = new DemoDapp.EthSendTransaction(transactionData);
            var (session, address, chainId) = GetCurrentAddress();
            var result = await _wc.Request<DemoDapp.EthSendTransaction, string>(session.Topic, request, chainId);
            return result;
        }

        public UniTask<HexBigInteger> EstimateGas(TransactionInput transactionInput)
		{
			return _web3Provider.TransactionManager.EstimateGasAsync(transactionInput).AsUniTask();
		}

		public async UniTask<BigInteger> GetBalance(string address)
		{
			if (address == null)
			{
				address = await GetDefaultAccount();
			}

			var balance = await _web3Provider.Eth.GetBalance.SendRequestAsync(address);
			return balance.Value;
		}

		public async UniTask<BigInteger> GetBlockNumber()
		{
			var blockNumber = await _web3Provider.Eth.Blocks.GetBlockNumber.SendRequestAsync();
			return blockNumber.Value;
		}

		public async UniTask<BigInteger> GetTransactionCount(string hash)
		{
			var blockNumber = await _web3Provider.Eth.Blocks.GetBlockTransactionCountByHash.SendRequestAsync(hash);
			return blockNumber.Value;
		}

		public async UniTask<BigInteger> GetTransactionCount(BlockParameter block)
		{
			var blockNumber = await _web3Provider.Eth.Blocks.GetBlockTransactionCountByNumber.SendRequestAsync(block);
			return blockNumber.Value;
		}

		public UniTask<BlockWithTransactions> GetBlockWithTransactions(string hash)
		{
			return _web3Provider.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(hash).AsUniTask();
		}

		public UniTask<BlockWithTransactions> GetBlockWithTransactions(BlockParameter block)
		{
			return _web3Provider.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(block).AsUniTask();
		}

		public UniTask<BlockWithTransactionHashes> GetBlockWithTransactionsHashes(string hash)
		{
			return _web3Provider.Eth.Blocks.GetBlockWithTransactionsHashesByHash.SendRequestAsync(hash).AsUniTask();
		}

		public UniTask<BlockWithTransactionHashes> GetBlockWithTransactionsHashes(BlockParameter block)
		{
			return _web3Provider.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(block).AsUniTask();
		}

        public (SessionStruct, string, string) GetCurrentAddress()
        {
            var currentSession = _wc.Session.Get(_wc.Session.Keys[0]);

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
}