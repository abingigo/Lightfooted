using UnityEngine;

public class rotatelight : MonoBehaviour
{
    public float rotspeed = 60f;

    void Update()
    {
        transform.Rotate(0f, 0f,rotspeed * Time.deltaTime);
    }
} 