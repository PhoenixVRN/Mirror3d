using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropDownController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NetworkPlayerSlotInformation networkPlayerSlotInformation = null;
    private Toggle[] toggle;

    public void OnPointerClick(PointerEventData _eventData)
    {
        //  If isLocalPlayer
        if (networkPlayerSlotInformation.NetworkRoomPlayerSlotPrefab.isLocalPlayer)
        {
            //  Get toggle from TMP_DropDown
            toggle = gameObject.GetComponentsInChildren<Toggle>();

            //  Send _eventData to networkPlayerSlotInformation and toogle[]
            networkPlayerSlotInformation.OnPointerClick(_eventData, toggle);
        }
    }
}
