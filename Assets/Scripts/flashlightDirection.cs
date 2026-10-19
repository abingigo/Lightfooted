using UnityEngine;
using UnityEngine.Rendering.Universal;

public class flashlightDirection : MonoBehaviour
{
    Light2D light2D;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        light2D.enabled = false;
    }

    void Update()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float x = mouse.x - transform.position.x;
        float y = mouse.y -  transform.position.y;
        float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg - 90f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        light2D.enabled = Input.GetMouseButton(0);
    }
}