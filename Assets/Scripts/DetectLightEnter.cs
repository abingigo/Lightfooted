using UnityEngine;
//https://docs.unity3d.com/ScriptReference/Collider2D.OnTriggerExit2D.html
public class DetectlLightEnter : MonoBehaviour
{
    bool characterInQuicksand; 

    void OnTriggerExit2D( Collider2D other)
    {
        if (!enabled) return; 
        characterInQuicksand = false;
        if (other.CompareTag("Player"))
        {
            other.enabled = false;
        }
    }
}
