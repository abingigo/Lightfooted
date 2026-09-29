using UnityEngine;

public enum Direction { Left, Right }

public class MovingLight : MonoBehaviour
{
    public Vector3 startingPosition;
    public Vector3 endingPosition;

    public Direction d;
    public float speed;

    void Update()
    {
        if (transform.position.x >= endingPosition.x)
        {
            d = Direction.Left;
        }
        else if (transform.position.x <= startingPosition.x)
        {
            d = Direction.Right;
        }

        float move = (d == Direction.Left) ? -speed : speed;
        transform.Translate(Vector3.right * move * Time.deltaTime, Space.World);
    }
}