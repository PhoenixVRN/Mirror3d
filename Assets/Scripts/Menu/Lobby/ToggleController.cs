using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleController : MonoBehaviour
{
    [Header("Var")]
    private int index = -1;
    private NetworkPlayerSlotInformation networkPlayerSlotInformation = null;

    private EventTrigger.Entry entry = new EventTrigger.Entry();

    public void StartInitialization(NetworkPlayerSlotInformation _networkPlayerSlotInformation, int _index)
    {
        //  Cache
        index = _index;
        networkPlayerSlotInformation = _networkPlayerSlotInformation;

        //  Add EventTrigger
        EventTrigger _eventTrigger = gameObject.AddComponent<EventTrigger>();

        //  Add event PointerClick
        entry.eventID = EventTriggerType.PointerClick;

        //  Add Listener and applt trigger
        entry.callback.AddListener((data) => { OnToggleClicked(); });
        _eventTrigger.triggers.Add(entry);
    }

    private void OnToggleClicked()
    {
        //  Send result to main networkPlayerSlotInformation
        networkPlayerSlotInformation.OnToggleClicked(index);
    }

    private void OnDestroy()
    {
        //  Remove all listeners
        entry.callback.RemoveAllListeners();
    }
}
