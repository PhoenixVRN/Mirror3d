using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NetworkPlayerSlotInformation : MonoBehaviour
{
    [Header("Player Information")]
    public NetworkRoomPlayerCustom NetworkRoomPlayerSlotPrefab = null;
    private NetworkPlayerInformation networkPlayerInformation = null;

    [Header("Player Slot UI")]
    public Image PlayerSlotBackground = null;
    public RawImage PlayerSlotAvatar = null;
    public TMP_Text PlayerSlotName = null;
    public TMP_Text PlayerSlotReady = null;

    public GameObject TeamDropDownUI = null;
    public TMP_Dropdown TeamDropDown = null;

    public GameObject KickButtonUI = null;
    public Button KickButton = null;

    public void DisableUI()
    {
        //  Disable UI
        PlayerSlotBackground.enabled = false;
        PlayerSlotAvatar.enabled = false;
        PlayerSlotName.enabled = false;
        PlayerSlotReady.enabled = false;
        TeamDropDownUI.SetActive(false);
    }

    public void EnableUI()
    {
        //  Enable UI
        PlayerSlotBackground.enabled = true;
        PlayerSlotAvatar.enabled = true;
        PlayerSlotName.enabled = true;
        PlayerSlotReady.enabled = true;
        TeamDropDownUI.SetActive(true);
    }

    private void OnDestroy()
    {
        TeamDropDown.onValueChanged.RemoveAllListeners();
    }

    public void OnPointerClick(PointerEventData _eventData, Toggle[] _toggle)
    {
        //  If null and isLocalPlayer
        if (networkPlayerInformation == null)
        {
            //  Cache NetworkPlayerInformation
            networkPlayerInformation = NetworkRoomPlayerSlotPrefab.GetComponent<NetworkPlayerInformation>();
        }

        //  Foreach toggle
        for (int _index = 0; _index < _toggle.Length; _index++)
        {
            //  Add ToggleController to toggle
            _toggle[_index].gameObject.AddComponent<ToggleController>();
            //  Cache ToggleController
            ToggleController _toggleController = _toggle[_index].GetComponent<ToggleController>();

            //  Start add listener
            _toggleController.StartInitialization(this, _index);
        }
    }

    //  Trigger listener 1 for all togglers
    public void OnToggleClicked(int _index)
    {
        //  If isServer
        if (NetworkRoomPlayerSlotPrefab.isServer)
        {
            //  If no slot in team
            if (!networkPlayerInformation.CheckIsFreeSlotInTeam(_index))
            {
                //  Set oldTeam (Due to TMP_DropDown event only - onValueChanged it's fix)
                TeamDropDown.value = networkPlayerInformation.Team;
            }
            else   //  Slot founded
            {
                //  Update playerSlotPrefab position in lobby
                networkPlayerInformation.OnServerTeamChanged(networkPlayerInformation.Team, _index, networkPlayerInformation.netIdentity.connectionToClient);
            }
        }
        else
        {
            //  Set oldTeam (Due to TMP_DropDown event only - onValueChanged it's fix)
            TeamDropDown.value = networkPlayerInformation.Team;

            //  Send request for change Team\Slot to server
            networkPlayerInformation.CmdRequestToChangeTeamSlot(networkPlayerInformation.Team, _index, networkPlayerInformation.netIdentity.connectionToClient);
        }
    }
}
