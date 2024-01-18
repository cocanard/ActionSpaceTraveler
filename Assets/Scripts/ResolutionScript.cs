using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ResolutionScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Screen.SetResolution((int)DatasScript.settings.resolution.x, (int)DatasScript.settings.resolution.y, DatasScript.settings.fullscreen);
    }
}
