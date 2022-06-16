using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class NetworkButtonManager : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject MainMenuUI = null;
    public GameObject LobbyUI = null;

    [Header("Connect UI")]
    public GameObject ConnectUI = null;
    [SerializeField] private GameObject inputFieldAdressUI = null;
    [SerializeField] private TMP_InputField inputFieldAdress = null;
    [SerializeField] private GameObject joinButtonUI = null;
    //[SerializeField] private GameObject cancelButtonUI = null;
    [SerializeField] private GameObject attemtToConnectTextUI = null;
    [SerializeField] private TMP_Text attemtToConnectText = null;

    [Header("Var")]
    private bool isConnectInProgress = false;

#if UNITY_SERVER

    private void Start()
    {
        Debug.Log("Current scene - Main Menu | Lobby");

        HostServer();

        //  Set offlineScene to "MainMenu" (While 0 player dedicated load offlineScene)
        NetworkManager.singleton.offlineScene = "MainMenu";
    }

#endif

    //  Create game and join to lobby.
    public void HostServerAndLocalClient()
    {
        // Check for current host status
        if (!NetworkClient.active)
        {
            // WebGL can't be a host
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                //  Disable main menu UI
                MainMenuUI.SetActive(false);
                //  Enable lobby UI
                LobbyUI.SetActive(true);

                NetworkManager.singleton.StartHost();
            }
        }
    }

    //  Create game in dedicated mode
    public void HostServer()
    {
        // Check for current host status
        if (!NetworkClient.active)
        {
            // WebGL can't be a host
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                //  Disable main menu UI
                MainMenuUI.SetActive(false);
                //  Enable lobby UI
                LobbyUI.SetActive(true);
                //  Disable lobby ReadyButtonUI
                GetComponent<NetworkLobbyManager>().ReadyButtonUI.SetActive(false);

                NetworkManager.singleton.StartServer();
            }
        }
    }

    // Connect to lobby or game.
    public void ConnectToServer()
    {
        //  If not connected to lobby\game and address text is not null or empty
        if (!NetworkServer.active && !string.IsNullOrEmpty(inputFieldAdress.text))
        {
            NetworkManager.singleton.networkAddress = inputFieldAdress.text;

            //  Start connection procedure
            StartCoroutine(waitUntilNetworkClient());
        }
    }

    public void Stop()
    {
        //  Stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();

            //  Disable lobby UI
            LobbyUI.SetActive(false);
            //  Enable main menu UI
            MainMenuUI.SetActive(true);
        }
        else if (NetworkClient.isConnected) //  Stop client if client-only
        {
            //  Check is connection in progress
            if (isConnectInProgress)
            {
                CancelConnectionAttempt();

                StopCoroutine(waitUntilNetworkClient());
            }
            else
            {
                NetworkManager.singleton.StopClient();
            }
        }
        else if (NetworkServer.active)  //  Stop server if server-only
        {
            NetworkManager.singleton.StopServer();

            //  Disable lobby UI
            LobbyUI.SetActive(false);
            //  Enable main menu UI
            MainMenuUI.SetActive(true);
            //  Enable lobby ReadyButtonUI
            GetComponent<NetworkLobbyManager>().ReadyButtonUI.SetActive(true);
        }
    }

    public void CancelConnectionAttempt()
    {
        //  Stop connection attempt (WARNING! debug.log connect timeout is - ok, and be aware of null while timeout)
        NetworkManager.singleton.StopClient();

        //  Disable state in progress
        isConnectInProgress = false;
        //  Enable join UI
        joinButtonUI.SetActive(true);
        //  Enable input adress field UI
        inputFieldAdressUI.SetActive(true);
    }

    private IEnumerator waitUntilNetworkClient()
    {
        //  If not networkclient active, try to connect
        if (!NetworkClient.active)
        {
            NetworkManager.singleton.StartClient();

            //  Enable state in progress
            isConnectInProgress = true;

            //  Disable input adress field UI
            inputFieldAdressUI.SetActive(false);
            //  Disable connect UI
            joinButtonUI.SetActive(false);
            //  Enable cancel connect UI
            //cancelButtonUI.SetActive(true);
            //  Set connect info in text
            attemtToConnectText.text = "Attempt to connect " + inputFieldAdress.text;
            //  Enable attemt to connect text UI
            attemtToConnectTextUI.SetActive(true);

            yield return new WaitUntil(() => !NetworkClient.active || NetworkClient.isConnected);

            //  Disable state in progress
            isConnectInProgress = false;

            //  If succeeded connect
            if (NetworkClient.isConnected)
            {
                //  Disable connect UI
                ConnectUI.SetActive(false);

                //  Enable lobby UI
                LobbyUI.SetActive(true);
                //  Disable main menu UI
                MainMenuUI.SetActive(false);
            }

            //  Disable cancel connect UI
            //cancelButtonUI.SetActive(false);
            //  Disable attemt to connect text UI
            attemtToConnectTextUI.SetActive(false);
            //  Clear connect info text
            attemtToConnectText.text = string.Empty;
            //  Enable join UI
            joinButtonUI.SetActive(true);
            //  Enable input adress field UI
            inputFieldAdressUI.SetActive(true);
        }
        else  //    You already try to connect
        {
            Debug.Log("You already try to connect");
        }
    }
}
