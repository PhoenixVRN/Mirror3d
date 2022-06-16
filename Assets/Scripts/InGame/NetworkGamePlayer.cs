using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using Cinemachine;

public class NetworkGamePlayer : NetworkBehaviour
{
    [Header("Instantiate prefab")]
    [SerializeField] private GameObject playerFollowCameraPrefab = null;

    [Header("Camera")]
    [SerializeField] private GameObject playerFollowCamera = null;

    [Header("Script Link")]
    private NetworkGamePlayerMenuUI networkGamePlayerMenuUI = null;

    [Header("Game Object Link")]
    [Tooltip("In game Room Player")]
    public GameObject NetworkRoomPlayer = null;

    [Header("Var")]
    private NetworkRoomManager networkRoomManager = null;

    [SyncVar(hook = nameof(OnLobbySlotChanged))]
    [HideInInspector] public int LobbySlot = -1;

    public override void OnStartAuthority()
    {
        //  UI
        networkGamePlayerMenuUI = GetComponent<NetworkGamePlayerMenuUI>();
        networkGamePlayerMenuUI.enabled = true;

        networkGamePlayerMenuUI.PlayerCanvas.SetActive(true);
        networkGamePlayerMenuUI.EventSystem.SetActive(true);
    }

    #region Void

    public void Initialization()
    {
        //  Wait until LobbySlot sync
        StartCoroutine(waitUntilLobbySlotChanged());
    }

    private IEnumerator waitUntilLobbySlotChanged()
    {
        //  Wait for LobbySlot sync from server
        yield return new WaitUntil(() => LobbySlot != -1);

        //  Instantiate playerFollowCameraPrefab - Cinemachine
        playerFollowCamera = Instantiate(playerFollowCameraPrefab, transform.position, transform.transform.rotation);
        //  Activate
        playerFollowCamera.SetActive(true);

        //  If NetworkGamePlayerAvatar not initialized
        if (NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayerAvatar == null)
        {
            //  Wait until NetworkGamePlayerAvatarNotNull
            StartCoroutine(waitUntilNetworkGamePlayerAvatarNotNull());
        }
        else
        {
            //  Assign follow
            playerFollowCamera.GetComponent<CinemachineVirtualCamera>().Follow = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayerAvatar.GetComponent<ThirdPersonController>().CameraFollowTarget.transform;

            //  Enable stuff for local player        
            GetComponent<AudioListener>().enabled = true;
            GetComponent<CinemachineBrain>().enabled = true;

            //  Wait 1 frame and enable Camera (Need frame for Cinemachine camera initialization)
            StartCoroutine(waitEndOfFrame());
        }
    }

    private IEnumerator waitUntilNetworkGamePlayerAvatarNotNull()
    {
        //  Wait for LobbySlot sync from server
        yield return new WaitUntil(() => LobbySlot != -1);

        //  Assign follow
        playerFollowCamera.GetComponent<CinemachineVirtualCamera>().Follow = NetworkRoomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayerAvatar.GetComponent<ThirdPersonController>().CameraFollowTarget.transform;

        //  Enable stuff for local player        
        GetComponent<AudioListener>().enabled = true;
        GetComponent<CinemachineBrain>().enabled = true;

        //  Wait 1 frame and enable Camera (Need frame for Cinemachine camera initialization)
        StartCoroutine(waitEndOfFrame());
    }

    private IEnumerator waitEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        GetComponent<Camera>().enabled = true;
    }

    private void LinkGamePlayerWithRoomPlayer()
    {
        //  Check for lobby scene
        networkRoomManager = NetworkManager.singleton as NetworkRoomManager;

        //  Find connectionToServer and link
        foreach (var _roomPlayer in networkRoomManager.roomSlots)
        {
            //  If same LobbySlot
            if (_roomPlayer.GetComponent<NetworkPlayerInformation>().LobbySlot == LobbySlot)
            {
                //  Link
                _roomPlayer.GetComponent<NetworkPlayerInformation>().NetworkGamePlayer = this.gameObject;
                NetworkRoomPlayer = _roomPlayer.gameObject;

                //  Already found
                break;
            }
        }
    }

    #endregion

    #region Hook

    public void OnLobbySlotChanged(int _oldLobbySlot, int _newLobbySlot)
    {
        //  If incorrect value - Return
        if (_newLobbySlot <= -1)
        {
            return;
        }

        //  If not isServer
        if (!isServer)
        {
            LinkGamePlayerWithRoomPlayer();
        }
    }

    #endregion

}
