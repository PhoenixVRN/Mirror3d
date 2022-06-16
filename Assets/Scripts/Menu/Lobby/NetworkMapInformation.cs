using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class NetworkMapInformation : NetworkBehaviour
{
    [Header("Map Settings")]
    [SyncVar]
    [Tooltip("This name see all players in lobby")]
    [HideInInspector] public string MapName;
    [SyncVar]
    [Tooltip("Maximum players on map")]
    [HideInInspector] public int MaxPlayers;
    [SyncVar]
    [Tooltip("Team count in lobby")]
    public List<string> TeamCount = new List<string>();
    [SyncVar]
    [Tooltip("Team slot count foreach TeamCount. Example: TeamCount[0] = TeamSlotCount[0] (Demons - 3 slots)")]
    public List<int> TeamSlotCount = new List<int>();

    [Header("Game Manager")]
    private NetworkRoomManager networkRoomManager = null;

    [ServerCallback]
    private void Awake()
    {
        //  Cache networkRoomManager
        networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

        //  If lobby
        if (SceneManager.GetActiveScene().path == networkRoomManager.RoomScene)
        {
            //  Assign Map Settings to roomPlayer
            MapName = NetworkLobbyManager.Singleton.MapSettingsScript.MapName;
            MaxPlayers = NetworkLobbyManager.Singleton.MapSettingsScript.MaxPlayers;
            TeamCount = NetworkLobbyManager.Singleton.MapSettingsScript.TeamCount;
            TeamSlotCount = NetworkLobbyManager.Singleton.MapSettingsScript.TeamSlotCount;
        }
    }
}
