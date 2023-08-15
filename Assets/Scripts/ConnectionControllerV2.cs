using System;
using AnkrSDK.Utils;
using AnkrSDK.WalletConnectSharp.Core;
using AnkrSDK.WalletConnectSharp.Unity.Events;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using AnkrSDK.WalletConnect2.Events;
using AnkrSDK.WalletConnect2;

namespace AnkrSDK.UI
{
    public class ConnectionControllerV2 : MonoBehaviour
    {
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private Button _loginButton;
        [SerializeField] private GameObject _sceneChooser;
        [SerializeField] private ChooseWalletScreen _chooseWalletScreen;
        [SerializeField] private AnkrSDK.Utils.UI.QRCodeImage _qrCodeImage;
        private WalletConnect2.WalletConnect2 WalletConnect2 => ConnectProvider<WalletConnect2.WalletConnect2>.GetConnect();
        private async void Start()
        {
            if (Application.isEditor || Application.platform != RuntimePlatform.WebGLPlayer)
            {
                await WalletConnect2.Connect();
            }
        }

        private void OnEnable()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                _loginButton.gameObject.SetActive(false);
            }
            else
            {
                _sceneChooser.SetActive(false);
                _loginButton.onClick.AddListener(GetLoginAction());
                _loginButton.gameObject.SetActive(false);
                SubscribeToWalletEvents();
                UpdateLoginButtonState();
            }
        }

        private UnityAction GetLoginAction()
        {
            if (!Application.isEditor)
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.IPhonePlayer:
                        return () => _chooseWalletScreen.Activate(WalletConnect2.OpenDeepLink);
                    case RuntimePlatform.Android:
                        return WalletConnect2.OpenDeepLink;
                }
            }

            return () =>
            {
                _qrCodeImage.UpdateQRCode(WalletConnect2.ConnectURL);
                _qrCodeImage.SetImageActive(true);
            };
        }

        private void SubscribeToWalletEvents()
        {
            WalletConnect2.SessionStatusUpdated += SessionStatusUpdated;
        }

        private void UnsubscribeFromWalletEvents()
        {
            WalletConnect2.SessionStatusUpdated -= SessionStatusUpdated;
        }

        private void OnDisable()
        {
            UnsubscribeFromWalletEvents();
        }

        private void SessionStatusUpdated(WalletConnect2TransitionBase walletConnectTransition)
        {
            Debug.Log($"Prev: {walletConnectTransition.PreviousStatus}, New: {walletConnectTransition.NewStatus}");
            UpdateLoginButtonState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("ACC: " + WalletConnect2.GetDefaultAccount());
            }
        }

        private void UpdateLoginButtonState()
        {
            var status = WalletConnect2.Status;

            if (status == WalletConnect2Status.Uninitialized)
            {
                return;
            }

            var walletConnected = status == WalletConnect2Status.WalletConnected;
            _sceneChooser.SetActive(walletConnected);
            _chooseWalletScreen.SetActive(!walletConnected);

            bool waitingForLoginInput = status == WalletConnect2Status.ConnectionRequestSent;

            _loginButton.gameObject.SetActive(waitingForLoginInput);
            _stateText.gameObject.SetActive(!waitingForLoginInput && !walletConnected);

            _qrCodeImage.SetImageActive(false);

            if (!waitingForLoginInput)
            {
                switch (status)
                {
                    case WalletConnect2Status.Uninitialized:
                        _stateText.text = "Uninitialized";
                        break;
                    case WalletConnect2Status.Disconnected:
                        _stateText.text = "Disconnected";
                        break;
                    case WalletConnect2Status.ConnectionRequestSent:
                        _stateText.text = "ConnectionRequestSent";
                        break;
                    case WalletConnect2Status.WalletConnected:
                        _stateText.text = "WalletConnected";
                        break;
                    case WalletConnect2Status.AnythingConnected:
                        _stateText.text = "AnythingConnected";
                        break;
                    default:
                        break;
                }

                //switch (status)
                //{
                //    case WalletConnectStatus.DisconnectedNoSession:
                //        {
                //            _stateText.text = "Disconnected";
                //            break;
                //        }
                //    case WalletConnectStatus.DisconnectedSessionCached:
                //        {
                //            _stateText.text = "Disconnected";
                //            break;
                //        }
                //    case WalletConnectStatus.TransportConnected:
                //        {
                //            _stateText.text = "Transport Connected";
                //            break;
                //        }
                //    case WalletConnectStatus.WalletConnected:
                //        {
                //            _stateText.text = "Connected";
                //            break;
                //        }
                //}
            }
        }
    }
}