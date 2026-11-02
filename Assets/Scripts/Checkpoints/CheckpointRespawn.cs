using Core;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CheckpointRespawn : MonoBehaviour
{
    IGameService game;
    Rigidbody2D body;

    void Awake() => body = GetComponent<Rigidbody2D>();

    void OnEnable()
    {
        if (ServiceRegistry.TryGet(out game)) game.Respawned += MoveTo;
    }

    void OnDisable()
    {
        if (game != null) game.Respawned -= MoveTo;
        game = null;
    }

    void MoveTo(Vector3 point)
    {
        transform.position = point;
        body.position = point;
        body.linearVelocity = Vector2.zero;

        foreach (var follow in FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
            if (follow.player == transform)
                follow.transform.position = point + follow.offset;
    }
}
