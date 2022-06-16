using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkPlayerInformation : NetworkBehaviour
{
    [Header("Player Settings")]
    public Texture2D PlayerAvatar;
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string PlayerName;
    [SyncVar(hook = nameof(OnTeamChanged))]
    public int Team;
    [SyncVar(hook = nameof(OnLobbySlotChanged))]
    public int LobbySlot;

    [Header("Game Manager")]
    private GameObject gameManager = null;
    private NetworkRoomManager networkRoomManager = null;
    private NetworkLobbyManager networkLobbyManager = null;

    [Header("Script Link")]
    [SerializeField] private NetworkRoomPlayerCustom networkRoomPlayerCustom = null;

    [Header("Game Object Link")]
    public GameObject NetworkGamePlayer = null;
    public GameObject NetworkGamePlayerAvatar = null;

    [Header("Var")]
    private bool isIninitalizationFinished = false;
    private bool isNewClient = true;
    private bool isAvatarFullyUploaded = false;
        
    public void StartInitialization()
    {
        //  Check for lobby scene
        networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

        if (networkRoomManager)
        {
            //  Cache game manager
            gameManager = NetworkLobbyManager.Singleton.gameObject;

            if (isLocalPlayer)
            {
                //  Return if not lobby scene
                if (!NetworkManager.IsSceneActive(networkRoomManager.RoomScene)) { return; }

                //  Set PlayerAvatar from JsonUtils
                PlayerAvatar = gameManager.GetComponent<Profile>().ProfileAvatar;
                //  Set PlayerName from JsonUtils
                PlayerName = gameManager.GetComponent<Profile>().ProfileName;

                //  Update PlayerSlotAvatar (For isLocalPlayer because Texture2D can't be a syncvar)
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotAvatar.texture = PlayerAvatar;

                //  If isLocalPlayer and not isServer
                if (!isServer)
                {
                    //  Send PlayerName to server
                    CmdSendName(PlayerName);

                    byte[] _profileAvatarByte = PlayerAvatar.EncodeToPNG();

                    CmdSendAvatar(_profileAvatarByte);
                }
                else   //  If isServer
                {
                    //  Set isAvatarFullyUploaded (For server not needed to upload avatar)
                    isAvatarFullyUploaded = true;
                }
            }
            else
            {
                //  If not isLocalPlayer and not isServer
                if (!isServer)
                {
                    //  Get PlayerSlotAvatar from server
                    CmdGetAvatar(netIdentity.connectionToClient);
                }
            }

            //  Update PlayerSlotName (Because it syncvar)
            networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotName.text = PlayerName;
            //  Update TeamDropDown.value
            networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().TeamDropDown.value = Team;
            //  Update PlayerSlotReady text to readyToBegin state (While readyToBegin - true "<color=green>Ready</color>" : false - "<color=red>Not Ready</color>")
            networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotReady.text = networkRoomPlayerCustom.readyToBegin ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";

            //  Cache NetworkLobbyManager
            networkLobbyManager = gameManager.GetComponent<NetworkLobbyManager>();

            //  If not isServer and isLocalPlayer
            if (!isServer && isLocalPlayer)
            {
                //  DisableUI for new connection (Wait for sync)
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().DisableUI();
                //  Update playerSlot SiblingIndex for joined player (Player wait sync - move at end of lobby)
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(9999);

                //  Add playerSlotPrefab to playerSlotGameObjectList
                networkLobbyManager.AddLobbyPlayerSlotPrefabToList(networkRoomPlayerCustom.PlayerSlotGameObject);
            }
            else if (!isServer && LobbySlot >= 0) //  If not isServer and synced with server
            {
                //  Update playerSlot SiblingIndex for player
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(LobbySlot);

                //  Update playerSlot SiblingIndex according to lobbyGameObjectList
                networkLobbyManager.UpdateClientLobbyPlayerSlotPosition(networkRoomPlayerCustom.PlayerSlotGameObject, LobbySlot);

                //  Add playerSlotPrefab to playerSlotGameObjectList
                networkLobbyManager.AddLobbyPlayerSlotPrefabToList(networkRoomPlayerCustom.PlayerSlotGameObject);

                //  Set isNewClient
                isNewClient = false;
            }
            else if (!isServer && LobbySlot == -1) //  If not isServer and not synced with server
            {
                //  DisableUI for new connection (Wait for sync)
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().DisableUI();
                //  Update playerSlot SiblingIndex for joined player (Player wait sync - move at end of lobby)
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(9999);

                //  Add playerSlotPrefab to playerSlotGameObjectList
                networkLobbyManager.AddLobbyPlayerSlotPrefabToList(networkRoomPlayerCustom.PlayerSlotGameObject);
            }

            //  Due to OnPlayerNameChanged triggered faster on client, need approve after initialization
            isIninitalizationFinished = true;
        }
    }

    #region Hook
        
    public void OnPlayerNameChanged(string oldPlayerName, string newPlayerName)
    {
        if (networkRoomManager)
        {
            //  Return if not lobby scene
            if (!NetworkManager.IsSceneActive(networkRoomManager.RoomScene)) { return; }

            //  Send to server if isLocalPlayer
            if (!isServer && isLocalPlayer && isIninitalizationFinished)
            {
                CmdSendName(newPlayerName);

                //  Update PlayerSlotName
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotName.text = newPlayerName;
            }
            else if (!isLocalPlayer && isIninitalizationFinished)   //  Update PlayerSlotName for other playerSlotPrefab
            {
                //  Update PlayerSlotName
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotName.text = newPlayerName;
            }
        }
    }
        
    public void OnTeamChanged(int oldTeam, int newTeam)
    {
        if (networkRoomManager)
        {
            //  Return if not lobby scene
            if (!NetworkManager.IsSceneActive(networkRoomManager.RoomScene)) { return; }

            //  Send to server if isLocalPlayer
            if (!isServer && isLocalPlayer && isIninitalizationFinished)
            {
                CmdSendTeam(newTeam);

                //  Update TeamDropDown.value
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().TeamDropDown.value = newTeam;
            }
            else if (!isLocalPlayer && isIninitalizationFinished)   //  Update PlayerSlotName for other playerSlotPrefab
            {
                //  Update TeamDropDown.value
                networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().TeamDropDown.value = newTeam;
            }
        }
    }
        
    public void OnServerTeamChanged(int _oldTeam, int _newTeam, NetworkConnection _networkConnection)
    {
        if (networkRoomManager)
        {
            //  If isLocalPlayer and isServer
            if (isLocalPlayer && isIninitalizationFinished)
            {
                //  Change playerSlotPrefab position in lobby
                networkLobbyManager.ChangeServerLobbyPlayerSlotPosition(_networkConnection, _oldTeam, _newTeam);
            }
        }
    }
        
    public void OnLobbySlotChanged(int _oldLobbySlot, int _newLobbySlot)
    {
        if (networkRoomManager)
        {
            //  Return if not lobby scene or isServer (Server side do slot stuff in NetworkLobbyManager)
            if (!NetworkManager.IsSceneActive(networkRoomManager.RoomScene) || isServer) { return; }

            //  Send to server if isLocalPlayer
            if (!isServer && isLocalPlayer && isIninitalizationFinished)
            {
                //  If new connection
                if (isNewClient)
                {
                    //  Update playerSlot SiblingIndex
                    networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(_newLobbySlot);

                    //  Update playerSlot SiblingIndex according to lobbyGameObjectList
                    networkLobbyManager.UpdateClientLobbyPlayerSlotPosition(networkRoomPlayerCustom.PlayerSlotGameObject, _newLobbySlot);

                    //  EnableUI after slot synced
                    networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().EnableUI();

                    //  Set isNewClient
                    isNewClient = false;
                }
                else   //  Player in lobby change slot position
                {
                    //  Update playerSlot SiblingIndex according to lobbyGameObjectList
                    networkLobbyManager.ChangeClientLobbyPlayerSlotPosition(networkRoomPlayerCustom.PlayerSlotGameObject, _newLobbySlot);
                }
            }
            else if (!isLocalPlayer && isIninitalizationFinished)   //  Update PlayerSlotName for other playerSlotPrefab
            {
                //  If new connection
                if (isNewClient)
                {
                    //  Update playerSlot SiblingIndex
                    networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(_newLobbySlot);

                    //  Update playerSlot SiblingIndex according to lobbyGameObjectList
                    networkLobbyManager.UpdateClientLobbyPlayerSlotPosition(networkRoomPlayerCustom.PlayerSlotGameObject, _newLobbySlot);

                    //  EnableUI after slot synced
                    networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().EnableUI();

                    //  Set isNewClient
                    isNewClient = false;
                }
                else   //  Player in lobby change slot position
                {
                    //  Update playerSlot SiblingIndex according to lobbyGameObjectList
                    networkLobbyManager.ChangeClientLobbyPlayerSlotPosition(networkRoomPlayerCustom.PlayerSlotGameObject, _newLobbySlot);
                }
            }
        }
    }

    [ServerCallback]
    public bool CheckIsFreeSlotInTeam(int _teamCheck)
    {
        if (networkRoomManager && isIninitalizationFinished)
        {
            return networkLobbyManager.IsFreeSlotInTeam(_teamCheck);
        }

        //  In any case - true (isServer)
        return true;
    }

    #endregion

    #region Command

    [Command]
    private void CmdSendAvatar(byte[] _bytes)
    {
        Texture2D _avatarTexture = new Texture2D(2, 2);
        _avatarTexture.LoadImage(_bytes);
        _avatarTexture.Apply();

        //  Assign PlayerAvatar to received _avatarTexture
        PlayerAvatar = _avatarTexture;
        //  Update PlayerSlotAvatar
        networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotAvatar.texture = PlayerAvatar;

        //  Set isAvatarFullyUploaded
        isAvatarFullyUploaded = true;
    }

    [Command(requiresAuthority = false)]
    private void CmdGetAvatar(NetworkConnectionToClient _targetPlayer)
    {
        //  Wait until server get full PlayerAvatar from client and sent it requested client
        StartCoroutine(waitUntilAvatarFullyUploaded(_targetPlayer));
    }

    private IEnumerator waitUntilAvatarFullyUploaded(NetworkConnectionToClient _targetPlayer)
    {
        //  Wait until isAvatarFullyUploaded
        yield return new WaitUntil(() => isAvatarFullyUploaded);

        byte[] _playerAvatarByte = PlayerAvatar.EncodeToPNG();

        //  Send to _targetPlayer with _playerAvatarByte
        RpcSendAvatarToClient(_targetPlayer, _playerAvatarByte);
    }

    [Command]
    private void CmdSendName(string _name)
    {
        //  Set CmdSendName
        PlayerName = _name;

        //  If dedicated
        if (isServerOnly)
        {
            //  Update PlayerSlotName
            networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotName.text = _name;
        }
    }

    [Command]
    private void CmdSendTeam(int _teamValue)
    {
        //  Set Team
        Team = _teamValue;

        //  If dedicated
        if (isServerOnly)
        {
            //  Update TeamDropDown.value
            networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().TeamDropDown.value = _teamValue;
        }
    }

    //  Request for Team\Slot change is same (Client)
    [Command]
    public void CmdRequestToChangeTeamSlot(int _oldTeam, int _newTeam, NetworkConnectionToClient _networkConnection)
    {
        if (networkRoomManager)
        {
            //  If not isLocalPlayer and isServer
            if (!isLocalPlayer && isIninitalizationFinished)
            {
                //  Change playerSlotPrefab position in lobby
                networkLobbyManager.ChangeServerLobbyPlayerSlotPosition(_networkConnection, _oldTeam, _newTeam);
            }
        }
    }

    #endregion

    #region TargetRpc

    [TargetRpc]
    private void RpcSendAvatarToClient(NetworkConnection _targetPlayer, byte[] _bytes)
    {
        Texture2D _avatarTexture = new Texture2D(2, 2);
        _avatarTexture.LoadImage(_bytes);
        _avatarTexture.Apply();

        //  Assign PlayerAvatar to received _avatarTexture
        PlayerAvatar = _avatarTexture;

        //  Update PlayerSlotAvatar
        networkRoomPlayerCustom.PlayerSlotGameObject.GetComponent<NetworkPlayerSlotInformation>().PlayerSlotAvatar.texture = PlayerAvatar;
    }

    #endregion

}
