using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PingCounter : MonoBehaviour
{
    [Header("UI")]
    private TMP_Text pingText = null;

    [Header("Var")]
    float counterTime;

    [Tooltip("Update interval Ping UI")]
    private const float updateInterval = 0.5f;

    private void Awake()
    {
        pingText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        //  Each frame minus time to counter
        counterTime -= Time.deltaTime;

        // Interval ended - update GUI text and start new interval
        if (counterTime <= 0.0)
        {
            //  Update UI
            pingText.text = "RTT: " + Math.Round(NetworkTime.rtt * 1000) + " ms";

            //  Renew counter
            counterTime = updateInterval;
        }
    }
}
