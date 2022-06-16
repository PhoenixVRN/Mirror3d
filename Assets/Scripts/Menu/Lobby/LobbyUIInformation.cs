using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUIInformation : MonoBehaviour
{
    [Header("Var")]
    public int SiblingIndex;
    [Tooltip("-1 - Default value, 0 - TeamUI, 1 - PlayerSlot, 2 - PlayerSlotDummy")]
    public int UIType = -1;

    private void Start()
    {
        //  Get SiblingIndex
        SiblingIndex = transform.GetSiblingIndex();
    }

    public void SetSiblingIndex(int _indexNumber)
    {
        //  Set SiblingIndex for gameobject
        transform.SetSiblingIndex(_indexNumber);

        //  Set SiblingIndex
        SiblingIndex = _indexNumber;
    }
}
