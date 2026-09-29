using UnityEngine;

public class ControllableLight : MonoBehaviour
{
    public float turnSpeed = 720f;

    public float[] anglesClockwise = { 0f, -90f, 180f, 90f };

    int currentIndex = 0;
    float targetAngle;

    void Start()
    {
        targetAngle = anglesClockwise[currentIndex];
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))    SetDirection(0);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) SetDirection(1);
        else if (Input.GetKeyDown(KeyCode.DownArrow))  SetDirection(2);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))  SetDirection(3);

        if (Input.GetKeyDown(KeyCode.F))
        {
            SetDirection((currentIndex + 1) % anglesClockwise.Length);
        }

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    void SetDirection(int index)
    {
        currentIndex = index;
        targetAngle = anglesClockwise[currentIndex];
    }
}