using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshot : MonoBehaviour
{

    public int size = 1;
    public string path = "shoots/temp_ss";
    public bool takeShoot = false;

    void Update()
    {
        
        if ( takeShoot )
        {
            ScreenCapture.CaptureScreenshot( $"{path}.png", size );
            takeShoot = false;
		}

    }
}
