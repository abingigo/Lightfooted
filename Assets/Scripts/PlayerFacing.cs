using Core;
using UnityEngine;

public sealed class PlayerFacing : MonoBehaviour
{
    [Tooltip("Mirrored to face left. Art inside it should face right. Never the Player root: its Left/Right colliders must not swap.")]
    [SerializeField] Transform visuals;

    public float Facing { get; private set; } = 1f;

    IGameService game;

    void Start()
    {
        ServiceRegistry.TryGet(out game);
        Apply();
    }

    void Update()
    {
        if (game != null && !game.IsGameplayActive) return;
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal == 0f || Mathf.Sign(horizontal) == Facing) return;
        Facing = Mathf.Sign(horizontal);
        Apply();
    }

    void Apply()
    {
        if (visuals == null) return;
        var scale = visuals.localScale;
        scale.x = Mathf.Abs(scale.x) * Facing;
        visuals.localScale = scale;
    }
}
