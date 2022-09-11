using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateFixAndroid : MonoBehaviour
{
    // to fix rate frame UI in Android Devices 
    private void Awake()
    {
        Application.targetFrameRate=60;
    }
}
