// SETTINGS FOR THE CAMERA

using UnityEngine;

public class CameraControl : MonoBehaviour
{                                 
    private Camera cameraObject;
    private void Awake()
    {
        cameraObject = GetComponentInChildren<Camera>();
        cameraObject.orthographic = true;
        cameraObject.orthographicSize = 45;
        transform.position = new Vector3(80, 45, 0);
    }
}