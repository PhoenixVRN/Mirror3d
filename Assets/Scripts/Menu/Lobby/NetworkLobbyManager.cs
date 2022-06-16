using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Singleton { get; private set; }

    [Header("Lobby UI")]
    public GameObject ScrollViewContent = null;
    public GameObject ReadyButtonUI = null;
    public Button ReadyButton = null;

    public GameObject StartGameButtonUI = null;
    public Button StartGameButton = null;

    public TMP_Text MapNameText = null;

    [Header("Map Settings")]
    [Tooltip("This name see all players in lobby")]
    public string MapName;
    [Tooltip("Team count in lobby")]
    public List<string> TeamCount = new List<string>();
    [Tooltip("Team slot count foreach TeamCount. Example: TeamCount[0] = TeamSlotCount[0] (Demons - 3 slots)")]
    public List<int> TeamSlotCount = new List<int>();

    [Header("Lobby Options")]
    [SerializeField] private bool isDynamicLobbyUI = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerSlotPrefab = null;
    [SerializeField] private GameObject playerSlotDummyPrefab = null;
    [SerializeField] private GameObject playerSlotDummyPrefabCache = null;
    [SerializeField] private GameObject playerTeamPrefab = null;
    [SerializeField] private List<GameObject> lobbyGameObjectList = new List<GameObject>();
    [SerializeField] private List<GameObject> playerTeamPrefabGameObjectList = new List<GameObject>();
    [SerializeField] private List<GameObject> playerSlotGameObjectList = new List<GameObject>();

    [Header("Var")]
    private bool isPlayerTeamPrefabInitialized = false;

    [Header("Linked Scripts")]
    public MapSettings MapSettingsScript = null;

    //  Script Execution Order settings (https://docs.unity3d.com/Manual/class-MonoManager.html)
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Singleton != null && Singleton != this)
        {
            Destroy(this);
        }
        else
        {
            Singleton = this;
        }
    }

    public void AddLobbyUI()
    {
        //  If isDynamicLobbyUI
        if (isDynamicLobbyUI)
        {
            //  Do dynamic playerSlotDummyPrefab
            playerSlotDummyPrefab = playerSlotDummyPrefabCache;
        }
        else
        {
            //  Do static playerSlotPrefab
            playerSlotDummyPrefab = playerSlotPrefab;
        }

        //  If Team more 0 and not isPlayerTeamPrefabInitialized
        if (TeamCount.Count > 0 && !isPlayerTeamPrefabInitialized)
        {
            //  For each index Instantiate _playerTeamPrefab
            for (int _index = 0; _index < TeamCount.Count; _index++)
            {
                //  Cache playerTeamPrefab and instantiate it (Foreach team)
                GameObject _playerTeamPrefab = Instantiate(playerTeamPrefab, playerTeamPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);
                //  Set name for _playerTeamPrefab
                //_playerTeamPrefab.name = TeamCount[_index];
                //  Change _playerTeamPrefab team name text
                _playerTeamPrefab.GetComponent<TMP_Text>().text = TeamCount[_index] + " " + "(" + TeamSlotCount[_index] + " " + "Slot)";

                //  Add _playerTeamPrefab in playerTeamPrefabGameObjectList
                playerTeamPrefabGameObjectList.Add(_playerTeamPrefab);
                //  Add _playerTeamPrefab in lobbyGameObjectList
                lobbyGameObjectList.Add(_playerTeamPrefab);

                //  Instantiate playerSlotDummyPrefab foreach TeamSlotCount
                for (int _indexDummy = 0; _indexDummy < TeamSlotCount[_index]; _indexDummy++)
                {
                    //  If isDynamicLobbyUI
                    if (isDynamicLobbyUI)
                    {
                        //  Cache playerSlotDummyPrefab and instantiate it
                        GameObject _playerSlotDummyPrefab = Instantiate(playerSlotDummyPrefab, playerSlotDummyPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

                        //  Change to PlayerSlotDummy type
                        _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().UIType = 2;

                        //  Add playerSlotDummyPrefab in lobbyGameObjectList
                        lobbyGameObjectList.Add(_playerSlotDummyPrefab);
                    }
                    else
                    {
                        //  Cache playerSlotDummyPrefab and instantiate it
                        GameObject _playerSlotDummyPrefab = Instantiate(playerSlotDummyPrefab, playerSlotDummyPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

#if UNITY_EDITOR
                        //  Change name to PlayerSlotDummy
                        _playerSlotDummyPrefab.name = "PlayerSlotDummy(Clone)";
#endif

                        //  Change to PlayerSlotDummy type
                        _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().UIType = 2;

                        //  Add playerSlotDummyPrefab in lobbyGameObjectList
                        lobbyGameObjectList.Add(_playerSlotDummyPrefab);
                    }
                }
            }

            //  Update Map Name
            MapNameText.text = MapName;

            //  Set isPlayerTeamPrefabInitialized
            isPlayerTeamPrefabInitialized = true;
        }
    }

    public void AddPlayerSlot(NetworkRoomPlayerCustom _networkRoomPlayer)
    {
        //  Lobby UI initialization, if wasn't initialized
        if (!isPlayerTeamPrefabInitialized)
        {
            AddLobbyUI();
        }

        //  Cache playerSlotPrefab and instantiate it
        GameObject _playerSlotPrefab = Instantiate(playerSlotPrefab, playerSlotPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

        //  Cache NetworkPlayerSlotInformation
        NetworkPlayerSlotInformation _networkPlayerSlotInformation = _playerSlotPrefab.GetComponent<NetworkPlayerSlotInformation>();
        //  Cache TransformSiblingIndex from _playerSlotPrefab
        LobbyUIInformation _lobbyUIInformation = _playerSlotPrefab.GetComponent<LobbyUIInformation>();

        //  Add NetworkRoomPlayer link to playerSlotPrefab
        _networkPlayerSlotInformation.NetworkRoomPlayerSlotPrefab = _networkRoomPlayer;
        //  Add _playerSlotPrefab link to NetworkRoomPlayerCustom - PlayerSlotGameObject
        _networkPlayerSlotInformation.NetworkRoomPlayerSlotPrefab.PlayerSlotGameObject = _playerSlotPrefab;

        //  If isServer
        if (_networkRoomPlayer.isServer)
        {
            //  If < 1 player
            if (playerSlotGameObjectList.Count < 1)
            {
                //  Destroy playerSlotDummyPrefab
                Destroy(lobbyGameObjectList[1]);

                //  Set first player SetSiblingIndex
                _lobbyUIInformation.SetSiblingIndex(1);
                //  Update SiblingIndex for host and isServer playerSlotPrefab
                _networkRoomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot = 1;
                //  Set Team
                _networkRoomPlayer.GetComponent<NetworkPlayerInformation>().Team = 0;

                //  Change to PlayerSlot type
                _lobbyUIInformation.UIType = 1;

                //  Update lobbyGameObjectList for host
                lobbyGameObjectList[1] = _playerSlotPrefab;

                //  Add _playerSlotPrefab in playerSlotGameObjectList 
                playerSlotGameObjectList.Add(_playerSlotPrefab);
            }
            else   //  If more than 1 player in lobby
            {
                //  Replace playerSlotDummyPrefab with playerSlotPrefab
                for (int _index = 0; _index < lobbyGameObjectList.Count; _index++)
                {
                    //  Cache SiblingIndex
                    int _siblingIndex = lobbyGameObjectList[_index].GetComponent<LobbyUIInformation>().SiblingIndex;
                    //  Cache IsPlayer 
                    int _uiType = lobbyGameObjectList[_index].GetComponent<LobbyUIInformation>().UIType;

                    //  If _uiType playerSlotDummyPrefab
                    if (_networkRoomPlayer.isServer && isDynamicLobbyUI && _uiType == 2 || _networkRoomPlayer.isServer && !isDynamicLobbyUI && _uiType == 2)
                    {
                        //  Destroy playerSlotDummyPrefab
                        Destroy(lobbyGameObjectList[_index]);

                        //  Set SetSiblingIndex for Client on isServer side
                        _lobbyUIInformation.SetSiblingIndex(_siblingIndex);
                        //  Change to PlayerSlot type
                        _lobbyUIInformation.UIType = 1;

                        //  Update SiblingIndex for Client (Client update value from server)
                        _networkRoomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot = _siblingIndex;

                        //  Replace playerSlotDummyPrefab with _playerSlotPrefab
                        lobbyGameObjectList[_index] = _playerSlotPrefab;

                        //  Add _playerSlotPrefab in playerSlotGameObjectList 
                        playerSlotGameObjectList.Add(_playerSlotPrefab);

                        //  Find Team
                        for (int _teamIndex = _siblingIndex; _teamIndex >= 0; _teamIndex--)
                        {
                            //  If _uiType TeamUIPrefab
                            if (_uiType == 0)
                            {
                                //  Set Team
                                _networkRoomPlayer.GetComponent<NetworkPlayerInformation>().Team = playerTeamPrefabGameObjectList.IndexOf(lobbyGameObjectList[_teamIndex]);

                                //  Already found team - break
                                break;
                            }
                        }
                        //  Already found slot - break
                        break;
                    }
                }
            }
        }

        //  Activate TeamDropDownUI
        _networkPlayerSlotInformation.TeamDropDownUI.SetActive(true);

        //  Fill TeamDropDown with data
        foreach (var _teamName in TeamCount)
        {
            _networkPlayerSlotInformation.TeamDropDown.options.Add(new TMP_Dropdown.OptionData() { text = _teamName });
        }

        //  If _isLocalPlayer
        if (_networkRoomPlayer.isLocalPlayer)
        {
            //  Set interactable
            _networkPlayerSlotInformation.TeamDropDown.interactable = true;
        }
    }

    public void DestroyPlayerSlot()
    {
        //  Destroy lobbyGameObjectList
        foreach (var _object in lobbyGameObjectList)
        {
            Destroy(_object);
        }

        //  Destroy playerSlotPrefab
        foreach (var _object in playerTeamPrefabGameObjectList)
        {
            Destroy(_object);
        }

        //  Destroy playerSlotPrefab
        foreach (var _object in playerSlotGameObjectList)
        {
            Destroy(_object);
        }

        //  Set to false
        isPlayerTeamPrefabInitialized = false;

        //  Clear list
        lobbyGameObjectList.Clear();
        playerTeamPrefabGameObjectList.Clear();
        playerSlotGameObjectList.Clear();
    }

    public void OnStopClient(int _siblingIndex, GameObject _object)
    {
        //  If players more than 1
        if (playerSlotGameObjectList.Count > 0)
        {
            //  Cache playerSlotDummyPrefab and instantiate it
            GameObject _playerSlotDummyPrefab = Instantiate(playerSlotDummyPrefab, playerSlotDummyPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

#if UNITY_EDITOR
            //  If not isDynamicLobbyUI (Static)
            if (!isDynamicLobbyUI)
            {
                //  Change name to PlayerSlotDummy
                _playerSlotDummyPrefab.name = "PlayerSlotDummy(Clone)";
            }
#endif

            //  Set _siblingIndex
            _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().SetSiblingIndex(_siblingIndex);
            //  Change to PlayerSlotDummy type
            _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().UIType = 2;

            //  Add _playerSlotDummyPrefab in lobbyGameObjectList (Where was playerSlotPrefab)
            lobbyGameObjectList[_siblingIndex] = _playerSlotDummyPrefab;

            //  Remove _object from playerSlotPrefab
            playerSlotGameObjectList.Remove(_object);
        }
    }

    public void AddLobbyPlayerSlotPrefabToList(GameObject _playerSlotPrefab)
    {
        //  Add _playerSlotPrefab in playerSlotGameObjectList 
        playerSlotGameObjectList.Add(_playerSlotPrefab);
    }

    #region Client

    public void UpdateLobbyPlayerSlotPosition()
    {
        //  Update playerSlot SiblingIndex according to lobbyGameObjectList
        for (int _index = 0; _index < lobbyGameObjectList.Count; _index++)
        {
            //  Cache gameobject UI
            GameObject _lobbyGameObject = lobbyGameObjectList[_index];

            //  Update only playerSlotPrefab
            if (_lobbyGameObject.GetComponent<NetworkPlayerSlotInformation>() != null && _lobbyGameObject.GetComponent<LobbyUIInformation>().UIType == 1)
            {
                //  Update LobbySlot
                _lobbyGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(_lobbyGameObject.GetComponent<NetworkPlayerSlotInformation>().NetworkRoomPlayerSlotPrefab.GetComponent<NetworkPlayerInformation>().LobbySlot);
            }
            else   //  TeamUI
            {
                _lobbyGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(_index);
            }
        }
    }

    public void UpdateClientLobbyPlayerSlotPosition(GameObject _playerSlotPrefab, int _newLobbySlot)
    {
        //  Destroy playerSlotDummyPrefab
        Destroy(lobbyGameObjectList[_newLobbySlot]);

        //  Replace playerSlotDummyPrefab with _playerSlotPrefab
        lobbyGameObjectList[_newLobbySlot] = _playerSlotPrefab;

        //  Update LobbyUI according to lobbyGameObjectList
        UpdateLobbyPlayerSlotPosition();
    }

    //  Client change playerSlotPrefab team
    public void ChangeClientLobbyPlayerSlotPosition(GameObject _playerSlotPrefab, int _newLobbySlot)
    {
        //  Cache playerSlotDummyPrefab and instantiate it
        GameObject _playerSlotDummyPrefab = Instantiate(playerSlotDummyPrefab, playerSlotDummyPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

#if UNITY_EDITOR
        //  If not isDynamicLobbyUI (Static)
        if (!isDynamicLobbyUI)
        {
            //  Change name to PlayerSlotDummy
            _playerSlotDummyPrefab.name = "PlayerSlotDummy(Clone)";
        }
#endif

        //  Change to PlayerSlotDummy type
        _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().UIType = 2;
        //  Cache SiblingIndex _playerSlotPrefab
        int _previousPlayerSlotSiblingIndex = _playerSlotPrefab.GetComponent<LobbyUIInformation>().SiblingIndex;

        //  Change _playerSlotDummyPrefab to previous
        lobbyGameObjectList[_previousPlayerSlotSiblingIndex] = _playerSlotDummyPrefab;

        //  Update playerSlot SiblingIndex according to lobbyGameObjectList
        UpdateClientLobbyPlayerSlotPosition(_playerSlotPrefab, _newLobbySlot);
    }

    #endregion

    #region Server

    //  Server side update Lobby
    public void UpdateServerLobbyPlayerSlotPosition()
    {
        //  Update playerSlot SiblingIndex according to lobbyGameObjectList
        for (int _index = 0; _index < lobbyGameObjectList.Count; _index++)
        {
            //  Cache gameobject UI
            GameObject _lobbyGameObject = lobbyGameObjectList[_index];

            _lobbyGameObject.GetComponent<LobbyUIInformation>().SetSiblingIndex(_index);

            //  If slot playerSlotPrefab
            if (_lobbyGameObject.GetComponent<LobbyUIInformation>().UIType == 1)
            {
                _lobbyGameObject.GetComponent<NetworkPlayerSlotInformation>().NetworkRoomPlayerSlotPrefab.GetComponent<NetworkPlayerInformation>().LobbySlot = _index;
            }
        }
    }

    //  Server change playerSlotPrefab team
    public void ChangeServerLobbyPlayerSlotPosition(NetworkConnection _networkConnection, int _oldTeam, int _newTeam)
    {
        //  Cache NetworkPlayerInformation
        NetworkPlayerInformation _networkPlayerInformation = _networkConnection.identity.GetComponent<NetworkPlayerInformation>();
        //  Cache PlayerSlotGameObject
        GameObject _playerSlotPrefab = _networkConnection.identity.GetComponent<NetworkRoomPlayerCustom>().PlayerSlotGameObject;

        //  Get how many slots in team
        int _teamSlots = MapSettingsScript.TeamSlotCount[_newTeam];
        //  Get teamUI SiblingIndex
        int _teamUISiblingIndex = playerTeamPrefabGameObjectList[_newTeam].GetComponent<LobbyUIInformation>().SiblingIndex;

        //  If _oldTeam equal _newTeam (Player want change slot in current team)
        if (_oldTeam == _newTeam)
        {
            //  Start from player slot
            _teamUISiblingIndex = _playerSlotPrefab.GetComponent<LobbyUIInformation>().SiblingIndex + 1;

            //  Check is player slot last in team (Yes - start from begining of the team. No - proceed from _playerSlotPrefab SiblingIndex)
            if (_teamUISiblingIndex == (playerTeamPrefabGameObjectList[_newTeam].GetComponent<LobbyUIInformation>().SiblingIndex + MapSettingsScript.TeamSlotCount[_newTeam]) + 1)
            {
                //  Start from team first slot
                _teamUISiblingIndex = playerTeamPrefabGameObjectList[_newTeam].GetComponent<LobbyUIInformation>().SiblingIndex + 1;
            }
        }
        else   //  Change team
        {
            //  Start from team first slot
            _teamUISiblingIndex += 1;
        }

        //  Foreach slot in team
        for (int _index = _teamUISiblingIndex; _index < _teamUISiblingIndex + _teamSlots; _index++)
        {
            //  Cache gameobject UI
            GameObject _lobbyGameObject = lobbyGameObjectList[_index];

            //  If playerSlotDummyPrefab
            if (_lobbyGameObject.GetComponent<LobbyUIInformation>().UIType == 2)
            {
                //  Destroy playerSlotDummyPrefab
                Destroy(_lobbyGameObject);

                //  Change _playerSlotPrefab to free slot
                lobbyGameObjectList[_index] = _playerSlotPrefab;

                //  Cache playerSlotDummyPrefab and instantiate it
                GameObject _playerSlotDummyPrefab = Instantiate(playerSlotDummyPrefab, playerSlotDummyPrefab.transform.position, Quaternion.identity, ScrollViewContent.transform);

#if UNITY_EDITOR
                //  If not isDynamicLobbyUI (Static)
                if (!isDynamicLobbyUI)
                {
                    //  Change name to PlayerSlotDummy
                    _playerSlotDummyPrefab.name = "PlayerSlotDummy(Clone)";
                }
#endif

                //  Cache SiblingIndex _playerSlotPrefab
                int _previousPlayerSlotSiblingIndex = _playerSlotPrefab.GetComponent<LobbyUIInformation>().SiblingIndex;
                //  Change to PlayerSlotDummy type
                _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().UIType = 2;

                //  Change _playerSlotDummyPrefab to previous
                lobbyGameObjectList[_previousPlayerSlotSiblingIndex] = _playerSlotDummyPrefab;

                //  Set SiblingIndex for _playerSlotDummyPrefab
                _playerSlotDummyPrefab.GetComponent<LobbyUIInformation>().SetSiblingIndex(_previousPlayerSlotSiblingIndex);
                //  Set SiblingIndex for _playerSlotPrefab
                _playerSlotPrefab.GetComponent<LobbyUIInformation>().SetSiblingIndex(_index);

                //  Update LobbySlot for host
                _networkPlayerInformation.LobbySlot = _index;
                //  Set Team to _networkPlayerInformation
                _networkPlayerInformation.Team = _newTeam;

                //  Update lobby due to possible desync
                UpdateServerLobbyPlayerSlotPosition();
                //  Already found slot - break
                break;
            }
        }
    }

    //  Check team for free slot
    public bool IsFreeSlotInTeam(int _teamCheck)
    {
        //  Get how many slots in team
        int _teamSlots = MapSettingsScript.TeamSlotCount[_teamCheck];
        //  Get teamUI SiblingIndex
        int _teamUISiblingIndex = playerTeamPrefabGameObjectList[_teamCheck].GetComponent<LobbyUIInformation>().SiblingIndex;

        //  Start from player slot
        _teamUISiblingIndex += 1;

        //  Foreach slot in team
        for (int _index = _teamUISiblingIndex; _index < _teamUISiblingIndex + _teamSlots; _index++)
        {
            //  If playerSlotDummyPrefab
            if (lobbyGameObjectList[_index].GetComponent<LobbyUIInformation>().UIType == 2)
            {
                //  Free slot founded
                return true;
            }
        }
        //  No free slot
        return false;
    }

    #endregion

    public void ChangeLobbyType(TMP_Dropdown _dropdown)
    {
        if (_dropdown.value == 0)
        {
            isDynamicLobbyUI = true;
        }
        else
        {
            isDynamicLobbyUI = false;
        }
    }
}
