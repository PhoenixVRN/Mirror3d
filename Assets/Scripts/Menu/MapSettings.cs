using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MapSettings : MonoBehaviour
{
    [Header("Campain Map Settings")]    
    [Tooltip("This name see all players in lobby")]
    public string MapName = string.Empty;
    [Tooltip("Maximum players on map")]
    [HideInInspector] public int MaxPlayers;
    [Tooltip("Team count in lobby")]
    public List<string> TeamCount = new List<string>();
    [Tooltip("Team slot count foreach TeamCount. Example: TeamCount[0] = TeamSlotCount[0] (Demons - 3 slots)")]
    public List<int> TeamSlotCount = new List<int>();

    private void Start()
    {
        //  Sum of TeamSlotCount
        foreach (var _slotsNumberPerTeam in TeamSlotCount)
        {
            MaxPlayers += _slotsNumberPerTeam;
        }

        //  Set MaxPlayers to NetworkRoomManagerCustom
        NetworkManager.singleton.maxConnections = MaxPlayers;        
    }
}
