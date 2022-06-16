using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkGamePlayerDedicated : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject canvasUI = null;
    [SerializeField] private GameObject eventSystem = null;


#if !UNITY_SERVER || !UNITY_WEBGL

    [ServerCallback]
    private void Start()
    {
        //  If dedicated server
        if (isServerOnly)
        {
            //  Enable UI
            canvasUI.SetActive(true);
            eventSystem.SetActive(true);

            //  Enable camera
            GetComponent<Camera>().enabled = true;
            GetComponent<AudioListener>().enabled = true;

            //  Set oflineScene to MainMenu
            NetworkManager.singleton.offlineScene = "MainMenu";
        }
    }

    public void StopButtons()
    {
        //  Stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        //  Stop client if client-only
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        //  Stop server if server-only
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
    }

#endif

}
