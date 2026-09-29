using UnityEngine;
//https://docs.unity3d.com/ScriptReference/Collider2D.OnTriggerExit2D.html
public class DetectlLightOut : MonoBehaviour
{
    bool characterInQuicksand; 

    void OnTriggerEnter2D( Collider2D other)
    {
        characterInQuicksand = false;
        if (other.CompareTag("Player"))
        {
            other.enabled = true;
        }
    }
}
