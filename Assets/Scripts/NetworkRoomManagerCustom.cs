using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

//  Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-manager
//	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomManager.html

//	See Also: NetworkManager
//	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
//	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html

//  This is a specialized NetworkManager that includes a networked room.
//  The room has slots that track the joined players, and a maximum player count that is enforced.
//  It requires that the NetworkRoomPlayer component be on the room player objects.
//  NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class.

public class NetworkRoomManagerCustom : NetworkRoomManager
{
    #region Var

    [Header("Settings")]
    [Tooltip("Auto start when lobby ready")]
    public bool AutoStart = false;

    [Header("Game Manager")]
    private GameObject gameManager = null;
    private NetworkLobbyManager networkLobbyManager = null;

    [Header("Info")]
    private NetworkPlayerSlotInformation networkPlayerSlotInformation = null;

    [Header("Spawnble prefab")]
    [Tooltip("In game player prefab for player")]
    [SerializeField] private GameObject inGamePlayerPrefab = null;

    #endregion

    #region Server Callbacks

    //  This is called on the server when the server is started - including when a host is started.
    public override void OnRoomStartServer()
    {
        if (gameManager == null)
        {
            //  Cache lobby manager
            networkLobbyManager = NetworkLobbyManager.Singleton;
        }

        //  Link Map Settings (Server stuff, client sync via NetworkMapInformation on roomPlayerPrefab)
        networkLobbyManager.MapName = networkLobbyManager.MapSettingsScript.MapName;
        networkLobbyManager.TeamCount = networkLobbyManager.MapSettingsScript.TeamCount;
        networkLobbyManager.TeamSlotCount = networkLobbyManager.MapSettingsScript.TeamSlotCount;
        //  Lobby UI initialization
        networkLobbyManager.AddLobbyUI();

        //  Enable start button UI
        networkLobbyManager.StartGameButtonUI.SetActive(true);
        //  Add listener to start game button
        networkLobbyManager.StartGameButton.onClick.AddListener(OnStartGameButtonClick);
    }

    //  This is called on the server when the server is stopped - including when a host is stopped.
    public override void OnRoomStopServer()
    {
        //  If in lobby
        if (SceneManager.GetActiveScene().path == RoomScene)
        {
            //  Remove listener from start button
            networkLobbyManager.StartGameButton.onClick.RemoveListener(OnStartGameButtonClick);
            //  Disable start button
            networkLobbyManager.StartGameButtonUI.SetActive(false);

            //  Destroy all playerSlotPrefab
            networkLobbyManager.DestroyPlayerSlot();
        }
    }

    //  This is called on the host when a host is started.
    public override void OnRoomStartHost()
    {

    }

    //  This is called on the host when the host is stopped.
    public override void OnRoomStopHost()
    {

    }

    //  This is called on the server when a new client connects to the server.
    //  <param name="conn">The new connection.</param>
    public override void OnRoomServerConnect(NetworkConnection _conn)
    {
        //  If in lobby
        if (SceneManager.GetActiveScene().path == RoomScene)
        {
            //  Wait until server create RoomPlayer
            StartCoroutine(waitUntilPlayerLoaded(_conn));
        }
        else
        {
            //  Disconnect player - game in progress
            _conn.Disconnect();

#if UNITY_SERVER

            //  Info in server console
            Debug.Log(_conn + " was disconnected - game in progress");

#endif

        }

#if UNITY_SERVER

        //  Info in server console
        Debug.Log(_conn + " connected");

#endif

    }

    //  This is called on the server when a client disconnects.
    //  <param name="conn">The connection that disconnected.</param>
    public override void OnRoomServerDisconnect(NetworkConnection _conn)
    {
        //  If in lobby and not isLocalPlayer (Means isServer)
        if (_conn != null && SceneManager.GetActiveScene().path == RoomScene && !_conn.identity.isLocalPlayer)
        {
            //  Cache networkPlayerInformation networkRoomPlayerCustom
            NetworkRoomPlayerCustom _networkRoomPlayerCustom = _conn.identity.GetComponent<NetworkRoomPlayerCustom>();

            //  Remove listener from KickButton
            networkPlayerSlotInformation.KickButton.onClick.RemoveListener(_networkRoomPlayerCustom.OnKickButtonClick);

            //  Replace client with playerSlotDummyPrefab
            networkLobbyManager.OnStopClient(_networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SiblingIndex, _networkRoomPlayerCustom.PlayerSlotGameObject);

            //  Destroy slotPlayerPrefab
            Destroy(_networkRoomPlayerCustom.PlayerSlotGameObject);

            //  Update lobby due to possible desync
            networkLobbyManager.UpdateServerLobbyPlayerSlotPosition();
        }

#if UNITY_SERVER

        else if (SceneManager.GetActiveScene().path != RoomScene && !_conn.identity.isLocalPlayer && roomSlots.Count == 0)    //  If not in lobby and not isLocalPlayer (Means isServer) and 0 players
        {
            //  Stop server
            NetworkManager.singleton.StopServer();
        }

        //  Info in server console
        Debug.Log(_conn + " disconnected");

#endif

    }

    //  This is called on the server when a networked scene finishes loading.
    //  <param name="sceneName">Name of the new scene.</param>
    public override void OnRoomServerSceneChanged(string _sceneName)
    {

    }

    //  This allows customization of the creation of the room-player object on the server.
    //  <para>By default the roomPlayerPrefab is used to create the room-player, but this function allows that behaviour to be customized.</para>
    //  <param name="conn">The connection the player object is for.</param>
    //  <returns>The new room-player object.</returns>
    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection _conn)
    {

#if UNITY_SERVER

        //  Info in server console
        Debug.Log("Create roomPlayer for " + _conn + " succeeded");

#endif

        return base.OnRoomServerCreateRoomPlayer(_conn);
    }

    //  This allows customization of the creation of the GamePlayer object on the server.
    //  <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
    //  <param name="conn">The connection the player object is for.</param>
    //  <param name="roomPlayer">The room player object for this connection.</param>
    //  <returns>A new GamePlayer object.</returns>
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection _conn, GameObject _roomPlayer)
    {

#if UNITY_SERVER

        //  Info in server console
        Debug.Log("Create gamePlayer for " + _conn + " succeeded");

#endif

        return base.OnRoomServerCreateGamePlayer(_conn, _roomPlayer);
    }

    //  This allows customization of the creation of the GamePlayer object on the server.
    //  <para>This is only called for subsequent GamePlay scenes after the first one.</para>
    //  <para>See OnRoomServerCreateGamePlayer to customize the player object for the initial GamePlay scene.</para>
    //  <param name="conn">The connection the player object is for.</param>
    public override void OnRoomServerAddPlayer(NetworkConnection _conn)
    {
        base.OnRoomServerAddPlayer(_conn);
    }

    //  This is called on the server when it is told that a client has finished switching from the room scene to a game player scene.
    //  <para>When switching from the room, the room-player is replaced with a game-player object. This callback function gives an opportunity to apply state from the room-player to the game-player object.</para>
    //  <param name="conn">The connection of the player</param>
    //  <param name="roomPlayer">The room player object.</param>
    //  <param name="gamePlayer">The game player object.</param>
    //  <returns>False to not allow this player to replace the room player.</returns>
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection _conn, GameObject _roomPlayer, GameObject _gamePlayer)
    {
        //  Link _gamePlayer with _roomPlayer
        _gamePlayer.GetComponent<NetworkGamePlayer>().NetworkRoomPlayer = _roomPlayer;
        //  Link _roomPlayer with _gamePlayer
        _roomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayer = _gamePlayer;

        //  Need for link _gamePlayer and _roomPlayer
        _gamePlayer.GetComponent<NetworkGamePlayer>().LobbySlot = _roomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot;

        //  Instantiate - in game player prefab
        GameObject _playerAvatar = Instantiate(inGamePlayerPrefab, _gamePlayer.transform.position, _gamePlayer.transform.rotation);

        //  Link _playerAvatar with _roomPlayer
        _playerAvatar.GetComponent<ThirdPersonController>().NetworkRoomPlayer = _roomPlayer;
        //  Link _roomPlayer with _playerAvatar
        _roomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayerAvatar = _playerAvatar;

        //  Need for link _playerAvatar and _roomPlayer
        _playerAvatar.GetComponent<ThirdPersonController>().LobbySlot = _roomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot;

        //  Server side spawn and add owner
        NetworkServer.Spawn(_playerAvatar, _roomPlayer);

#if UNITY_SERVER

        //  Info in server console
        Debug.Log(_conn + " (" + _conn.identity.GetComponent<NetworkPlayerInformation>().PlayerName + ") " + " succeeded loaded into scene");

#endif

        return base.OnRoomServerSceneLoadedForPlayer(_conn, _roomPlayer, _gamePlayer);
    }

    //  This is called on the server when all the players in the room are ready.
    //  <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
    public override void OnRoomServerPlayersReady()
    {
        if (AutoStart)
        {
            base.OnRoomServerPlayersReady();
        }
    }

    //  This is called on the server when CheckReadyToBegin finds that players are not ready
    //  <para>May be called multiple times while not ready players are joining</para>
    public override void OnRoomServerPlayersNotReady()
    {

    }

    #endregion

    #region Client Callbacks

    //  This is a hook to allow custom behaviour when the game client enters the room.
    public override void OnRoomClientEnter()
    {

    }

    //  This is a hook to allow custom behaviour when the game client exits the room.
    public override void OnRoomClientExit()
    {

    }

    //  This is called on the client when it connects to server.
    public override void OnRoomClientConnect()
    {

    }

    //  This is called on the client when disconnected from a server.    
    public override void OnRoomClientDisconnect()
    {

    }

    //  This is called on the client when a client is started.
    public override void OnRoomStartClient()
    {

    }

    //  This is called on the client when the client stops.
    public override void OnRoomStopClient()
    {

    }

    //  This is called on the client when the client is finished loading a new networked scene.    
    public override void OnRoomClientSceneChanged()
    {

    }

    //  Called on the client when adding a player to the room fails.
    //  <para>This could be because the room is full, or the connection is not allowed to have more players.</para>
    public override void OnRoomClientAddPlayerFailed()
    {

    }

    #endregion

    #region Optional UI

    public override void OnGUI()
    {
        //base.OnGUI();
    }

    #endregion

    #region UI

    private void OnStartGameButtonClick()
    {
        if (allPlayersReady)
        {
            //  All players are readyToBegin, start the game
            ServerChangeScene(GameplayScene);
        }
    }

    #endregion

    #region Custom Replaces

    //  Override default things, i'm not use custom scene for lobby (Main menu is also a lobby)
    public override void OnStartServer()
    {
        OnRoomStartServer();
    }

    #endregion

    #region Void's

    //  Wait until server create RoomPlayer
    private IEnumerator waitUntilPlayerLoaded(NetworkConnection _conn)
    {
        yield return new WaitUntil(() => _conn.identity != null);

        //  Cache networkPlayerInformation networkRoomPlayerCustom
        NetworkRoomPlayerCustom _networkRoomPlayerCustom = _conn.identity.GetComponent<NetworkRoomPlayerCustom>();

        //  Add playerSlotPrefab to scene
        networkLobbyManager.AddPlayerSlot(_networkRoomPlayerCustom);

        //  Cache networkPlayerInformation
        NetworkPlayerInformation _networkPlayerInformation = _conn.identity.GetComponent<NetworkPlayerInformation>();
        //  After all sync complete StartInitialization
        _networkPlayerInformation.StartInitialization();
        //  Set isInitialized
        _networkRoomPlayerCustom.IsInitialized = true;

        //  Add kick button for non isLocalPlayer
        if (!_conn.identity.isLocalPlayer)
        {
            //  Cache
            networkPlayerSlotInformation = _networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>();

            //  Activate KickButtonUI for isServer for each client but not isLocalPlayer
            networkPlayerSlotInformation.KickButtonUI.SetActive(true);

            //  Add listener to KickButton
            networkPlayerSlotInformation.KickButton.onClick.AddListener(_networkRoomPlayerCustom.OnKickButtonClick);
        }

        //  Update lobby due to possible desync
        networkLobbyManager.UpdateServerLobbyPlayerSlotPosition();
    }

    #endregion

}
