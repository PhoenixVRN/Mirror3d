using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;

//  Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-player
//  API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomPlayer.html

public class NetworkRoomPlayerCustom : NetworkRoomPlayer
{
    #region Var    

    [Header("Game Manager")]
    private NetworkRoomManager networkRoomManager = null;
    private GameObject gameManager = null;

    private NetworkButtonManager networkButtonManager = null;
    private NetworkLobbyManager networkLobbyManager = null;

    [Header("Prefabs")]
    public GameObject PlayerSlotGameObject = null;

    [Header("Info")]
    private NetworkPlayerInformation networkPlayerInformation = null;

    [Header("Var")]
    private bool isReadyToBeginCustom = false;
    [Tooltip("Due to Hooks, that can called before OnStartClient must wait until OnStartClient")]
    public bool IsInitialized = false;

    #endregion

    #region Start & Stop Callbacks

    //  This is invoked for NetworkBehaviour objects when they become active on the server.
    //  <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    //  <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    public override void OnStartServer()
    {

    }

    //  Invoked on the server when the object is unspawned
    //  <para>Useful for saving object data in persistent storage</para>
    public override void OnStopServer()
    {

    }

    //  Called on every NetworkBehaviour when it is activated on a client.
    //  <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    public override void OnStartClient()
    {
        //  Cache for not isLocalPlayer
        if (!isLocalPlayer)
        {
            //  Cache networkRoomManager
            networkRoomManager = NetworkManager.singleton as NetworkRoomManager;
        }
        //  If in lobby
        if (SceneManager.GetActiveScene().path == networkRoomManager.RoomScene)
        {
            //  Cache for not isLocalPlayer
            if (!isLocalPlayer)
            {
                //  Cache gameManager
                gameManager = NetworkLobbyManager.Singleton.gameObject;
                //  Cache networkLobbyManager
                networkLobbyManager = gameManager.GetComponent<NetworkLobbyManager>();
            }

            if (isServer || isServerOnly)
            {
                //  Do on server side
            }
            else
            {
                //  Update Map Settings from synced NetworkMapInformation
                NetworkLobbyManager.Singleton.MapName = GetComponent<NetworkMapInformation>().MapName;
                NetworkLobbyManager.Singleton.TeamCount = GetComponent<NetworkMapInformation>().TeamCount;
                NetworkLobbyManager.Singleton.TeamSlotCount = GetComponent<NetworkMapInformation>().TeamSlotCount;

                //  Add playerSlotPrefab to scene
                networkLobbyManager.AddPlayerSlot(GetComponent<NetworkRoomPlayerCustom>());

                //  Cache networkPlayerInformation
                networkPlayerInformation = GetComponent<NetworkPlayerInformation>();
                //  After all sync complete StartInitialization
                networkPlayerInformation.StartInitialization();
                //  Set isInitialized
                IsInitialized = true;
            }
        }
    }

    //  This is invoked on clients when the server has caused this object to be destroyed.
    //  <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
    public override void OnStopClient()
    {
        //  If in lobby and not isServer (Server side "OnRoomServerDisconnect" in "NetworkRoomManagerCustom")
        if (SceneManager.GetActiveScene().path == networkRoomManager.RoomScene && !isServer)
        {
            //  If local player disconnected from lobby (By host disconnected or kicked) (Check for isServer because isServer - true for host or server builds)
            if (!isServer && isLocalPlayer)
            {
                //  Remove listener from ready button
                networkLobbyManager.ReadyButton.onClick.RemoveListener(OnReadyButtonClick);

                //  Disable lobby UI
                networkButtonManager.LobbyUI.SetActive(false);
                //  Enable connect UI
                networkButtonManager.ConnectUI.SetActive(true);
            }

            //  If not isLocalPlayer and playerSlotGameObject not null
            if (!isLocalPlayer && PlayerSlotGameObject != null)
            {
                //  Destroy slotPlayerPrefab
                Destroy(PlayerSlotGameObject);
            }

            //  If isLocalPlayer
            if (isLocalPlayer)
            {
                //  Remove listener from ready button
                networkLobbyManager.ReadyButton.onClick.RemoveListener(OnReadyButtonClick);

                //  Reset map Name
                networkLobbyManager.MapNameText.text = string.Empty;

                //  Destroy all playerSlotPrefab
                networkLobbyManager.DestroyPlayerSlot();
            }
            else   //  If not isLocalPlayer
            {
                //  Replace client with playerSlotDummyPrefab
                networkLobbyManager.OnStopClient(GetComponent<NetworkRoomPlayerCustom>().PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SiblingIndex, GetComponent<NetworkRoomPlayerCustom>().PlayerSlotGameObject);
            }
        }
    }

    //  Called when the local player object has been set up.
    //  <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    public override void OnStartLocalPlayer()
    {

    }

    //  This is invoked on behaviours that have authority, based on context and <see cref="NetworkIdentity.hasAuthority">NetworkIdentity.hasAuthority</see>.
    //  <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
    //  <para>When <see cref="NetworkIdentity.AssignClientAuthority"/> is called on the server, this will be called on the client that owns the object. When an object is spawned with <see cref="NetworkServer.Spawn">NetworkServer.Spawn</see> with a NetworkConnection parameter included, this will be called on the client that owns the object.</para>
    public override void OnStartAuthority()
    {
        //  Check for lobby scene
        networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

        //  If in lobby
        if (SceneManager.GetActiveScene().path == networkRoomManager.RoomScene)
        {
            //  Cache gameManager
            gameManager = NetworkLobbyManager.Singleton.gameObject;

            //  Get component from cached gameManager
            networkButtonManager = gameManager.GetComponent<NetworkButtonManager>();
            networkLobbyManager = gameManager.GetComponent<NetworkLobbyManager>();

            //  Add listener to ready button
            networkLobbyManager.ReadyButton.onClick.AddListener(OnReadyButtonClick);
        }
    }

    //  This is invoked on behaviours when authority is removed.
    //  <para>When NetworkIdentity.RemoveClientAuthority is called on the server, this will be called on the client that owns the object.</para>
    public override void OnStopAuthority()
    {

    }

    #endregion

    #region Room Client Callbacks

    //  This is a hook that is invoked on all player objects when entering the room.
    //  <para>Note: isLocalPlayer is not guaranteed to be set until OnStartLocalPlayer is called.</para>
    public override void OnClientEnterRoom()
    {

    }

    //  This is a hook that is invoked on all player objects when exiting the room.
    public override void OnClientExitRoom()
    {

    }

    #endregion

    #region SyncVar Hooks

    //  This is a hook that is invoked on clients when the index changes.
    //  <param name="oldIndex">The old index value</param>
    //  <param name="newIndex">The new index value</param>
    public override void IndexChanged(int _oldIndex, int _newIndex)
    {

    }

    //  This is a hook that is invoked on clients when a RoomPlayer switches between ready or not ready.
    //  <para>This function is called when the a client player calls SendReadyToBeginMessage() or SendNotReadyToBeginMessage().</para>
    //  <param name="oldReadyState">The old readyState value</param>
    //  <param name="newReadyState">The new readyState value</param>
    public override void ReadyStateChanged(bool _oldReadyState, bool _newReadyState)
    {
        //  Sync with dedicated server Ready\Not Ready
        if (isServerOnly && networkRoomManager == null)
        {
            //  Cache networkRoomManager
            networkRoomManager = NetworkManager.singleton as NetworkRoomManager;
        }

        //  If in lobby
        if (networkRoomManager != null && SceneManager.GetActiveScene().path == networkRoomManager.RoomScene)
        {
            //  Must wait until OnStartClient and synced all stuff
            if (IsInitialized)
            {
                //  If isLocalPlayer sync readyToBegin with readyToBeginCustom
                if (isLocalPlayer)
                {
                    isReadyToBeginCustom = readyToBegin;
                }

                //  Update PlayerSlotReady text to readyToBegin state (While readyToBegin - true "<color=green>Ready</color>" : false - "<color=red>Not Ready</color>")
                PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotReady.text = readyToBegin ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
            }
            else
            {
                //  Wait until isInitialized
                StartCoroutine(waitUntilIsInitialized());
            }

            //  If client
            if (isLocalPlayer && !isServer)
            {
                //  Due to hooks work only on client push ReadyStateChanged on dedicated side
                CmdReadyStateChanged(_oldReadyState, _newReadyState);
            }
        }
    }

    private IEnumerator waitUntilIsInitialized()
    {
        //  Wait until isInitialized
        yield return new WaitUntil(() => IsInitialized);

        //  If isLocalPlayer sync readyToBegin with readyToBeginCustom
        if (isLocalPlayer)
        {
            isReadyToBeginCustom = readyToBegin;
        }

        //  Update PlayerSlotReady text to readyToBegin state (While readyToBegin - true "<color=green>Ready</color>" : false - "<color=red>Not Ready</color>")
        PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotReady.text = readyToBegin ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
    }

    #endregion

    #region Optional UI

    public override void OnGUI()
    {
        //base.OnGUI();
    }

    #endregion

    #region Lobby UI

    private void OnReadyButtonClick()
    {
        isReadyToBeginCustom = !isReadyToBeginCustom;

        //  Send to server state of ready
        CmdChangeReadyState(isReadyToBeginCustom);
    }

    [Server]
    public void OnKickButtonClick()
    {
        //  Host kick client
        GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
    }

    #endregion

    #region Command

    [Command]
    private void CmdReadyStateChanged(bool _oldReadyState, bool _newReadyState)
    {
        ReadyStateChanged(_oldReadyState, _newReadyState);
    }

    #endregion

}
