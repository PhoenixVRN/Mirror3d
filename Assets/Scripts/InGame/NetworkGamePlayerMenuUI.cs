using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkGamePlayerMenuUI : NetworkBehaviour
{
    NetworkManager manager = null;

    public GameObject PlayerCanvas = null;
    public GameObject EventSystem = null;

    [ClientCallback]
    private void Start()
    {
        //  If isLocalPlayer
        if (isLocalPlayer)
        {
            //  Cache NetworkManager
            manager = NetworkManager.singleton;

            //  Set oflineScene to MainMenu
            manager.offlineScene = "MainMenu";
        }
    }

    public void StopButtons()
    {
        //  Stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            manager.StopHost();
        }
        //  Stop client if client-only
        else if (NetworkClient.isConnected)
        {
            manager.StopClient();
        }
        //  Stop server if server-only
        else if (NetworkServer.active)
        {
            manager.StopServer();
        }
    }
}
