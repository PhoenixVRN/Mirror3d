using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("UI")]
    private TMP_Text fpsText = null;

    [Header("Var")]
    private float fps;
    float counterTime;

    [Tooltip("Update interval FPS UI")]
    private const float updateInterval = 0.5f;

    private void Awake()
    {
        fpsText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        //  Each frame minus time to counter
        counterTime -= Time.deltaTime;

        // Interval ended - update GUI text and start new interval
        if (counterTime <= 0.0)
        {
            //  Caculate FPS
            fps = 1 / Time.unscaledDeltaTime;
            //  Update UI
            fpsText.text = Math.Ceiling(fps) + " FPS";

            //  Renew counter
            counterTime = updateInterval;
        }
    }
}
