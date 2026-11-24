using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using LightPlatformer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Reveals each key prompt when the player reaches its stretch of the level. Only that prompt
/// is on screen; it fades once its keys are pressed, stays faded for a few seconds, then
/// disappears for good. Reaching the next prompt removes the previous one straight away.
///
/// A prompt with ghost steps first holds the game and plays a see-through copy of the player
/// through those steps, showing the route. The copy runs on the player's own controller and
/// physics, so it moves exactly as the player would with the same keys.
public class TutorialPrompts : MonoBehaviour
{
    [System.Serializable]
    class GhostStep
    {
        [Tooltip("Seconds this step lasts.")]
        [Min(0f)] public float seconds = 0.5f;

        [Tooltip("Horizontal input held for the whole step: -1 left, 1 right, 0 none.")]
        [Range(-1f, 1f)] public float move = 1f;

        [Tooltip("Press jump at the start of this step.")]
        public bool jump;

        [Tooltip("Press dash (Left Shift) at the start of this step.")]
        public bool dash;
    }

    enum Keys
    {
        Move,
        Jump,
        Dash,
        JumpAndDash,
        // Nothing to press: the prompt counts as done once the player moves past its stretch.
        None
    }

    [System.Serializable]
    class Prompt
    {
        public TMP_Text text;
        public Keys keys;

        [Tooltip("Appears, at full strength, once the player's X reaches this value...")]
        public float startX;

        [Tooltip("...and fades if the player's X reaches this value before the keys are pressed.")]
        public float endX;

        [Tooltip("Where the route begins. The ghost appears on the player, walks here, then plays the steps. Only X is used.")]
        public Vector2 ghostStart;

        [Tooltip("Inputs the ghost plays when this prompt first appears. Empty = no ghost for this prompt.")]
        public GhostStep[] ghostSteps = { };

        [Tooltip("Play the ghost without leaving its route line on screen.")]
        public bool hidePath;

        [System.NonSerialized] public bool reached;
        [System.NonSerialized] public bool jumpPressed;
        [System.NonSerialized] public bool dashPressed;
        [System.NonSerialized] public bool done;
        [System.NonSerialized] public float dim;
        [System.NonSerialized] public float sinceDone;
        [System.NonSerialized] public float hide;
        [System.NonSerialized] public bool gone;
    }

    [SerializeField] Transform player;
    [SerializeField] Prompt[] prompts = { };

    [Tooltip("Opacity once the keys have been pressed. Low enough to sit back, high enough to read.")]
    [SerializeField, Range(0f, 1f)] float dimmedAlpha = 0.4f;

    [Tooltip("Size once the keys have been pressed.")]
    [SerializeField, Range(0.1f, 1f)] float dimmedScale = 0.8f;

    [Tooltip("Seconds the change takes.")]
    [SerializeField, Min(0.01f)] float fadeSeconds = 0.3f;

    [Tooltip("Seconds after the keys are pressed before the prompt disappears.")]
    [SerializeField, Min(0f)] float hideAfterSeconds = 3f;

    [Header("Challenge")]
    [SerializeField] GameObject challengeCoin;

    [Tooltip("Shown only while the coin is still there, whether it was just picked up or a checkpoint restored it as taken.")]
    [SerializeField] TMP_Text challengeLabel;

    [Header("Ghost demo")]
    [Tooltip("Follows the ghost while it plays, then goes back to the player.")]
    [SerializeField] CameraFollow cameraFollow;

    [Tooltip("Multiplies the player's colours; the alpha sets how see-through the ghost is.")]
    [SerializeField] Color ghostColor = new Color(0.6f, 0.9f, 1f, 0.5f);

    [Tooltip("Draw the ghost's route as a line that stays as long as its instruction's text.")]
    [SerializeField] bool ghostPath = true;

    [Tooltip("Material for the route line. An unlit sprite material keeps it visible in the dark.")]
    [SerializeField] Material ghostPathMaterial;

    [SerializeField, Min(0.01f)] float ghostPathWidth = 0.1f;

    [Tooltip("Marks where the ghost dashed, on its route line. Points right; flipped for dashes to the left. Empty = a drawn double chevron.")]
    [SerializeField] Sprite dashIcon;

    [Tooltip("Height of the dash icon in world units.")]
    [SerializeField, Min(0.05f)] float dashIconSize = 0.6f;

    [Tooltip("Seconds the ghost stays at the end of its route, after it stops, before the player can move.")]
    [SerializeField, Min(0f)] float ghostEndPause = 0.6f;

    [Tooltip("Longest the player waits after the ghost's last step for it to land and stop, in case it never does.")]
    [SerializeField, Min(0f)] float ghostSettleLimit = 3f;

    [Tooltip("How close (in units) the ghost must get to the route's start before the steps take over.")]
    [SerializeField, Min(0.01f)] float ghostStartTolerance = 0.1f;

    [Tooltip("Longest the ghost spends walking from the player to the route's start.")]
    [SerializeField, Min(0f)] float ghostApproachLimit = 3f;

    // Only these keep running on the ghost; everything else (pickups, checkpoints, death, lights it carries) is removed.
    static readonly HashSet<string> GhostKeeps = new() { "PlayerController", "LightExposure", "LightSolidity", "LightGravity" };

    Coroutine demo;
    GameObject ghost;
    bool holding;
    Rigidbody2D frozenBody;
    Vector2 frozenVelocity;
    Transform cameraTarget;

    LineRenderer path;
    Prompt pathPrompt;
    readonly List<Vector3> pathPoints = new();
    // Dash marks on the line: x, y and the dash direction (+1 right, -1 left) in z.
    readonly List<Vector3> pathDashes = new();
    readonly List<SpriteRenderer> dashIcons = new();
    static Sprite drawnDashIcon;

    void Update()
    {
        if (challengeLabel != null)
            challengeLabel.enabled = challengeCoin != null && challengeCoin.activeInHierarchy;

        if (player == null) return;

        bool inputBlocked = LightPlatformer.UI.GameMenuController.GameplayInputBlocked;
        float x = player.position.x;

        bool anyCurrent = false;
        foreach (var prompt in prompts)
            anyCurrent |= x >= prompt.startX && x < prompt.endX;

        foreach (var prompt in prompts)
        {
            if (prompt.text == null || prompt.gone) continue;

            bool current = x >= prompt.startX && x < prompt.endX;

            // The next prompt takes over at once, so two never share the screen.
            if (prompt.reached && !current && anyCurrent)
            {
                prompt.gone = true;
                prompt.text.enabled = false;
                continue;
            }

            bool firstTime = current && !prompt.reached;
            prompt.reached |= current;
            if (firstTime) MaybePlayGhost(prompt);

            // Respawning past a prompt that hasn't been seen on this load: nothing to show.
            if (!prompt.reached) prompt.gone = x >= prompt.endX;
            prompt.text.enabled = prompt.reached;
            if (!prompt.reached) continue;

            // A prompt with no keys is done only once the player is through it, not when they step back.
            if (!current && (prompt.keys != Keys.None || x >= prompt.endX)) prompt.done = true;
            else if (!inputBlocked) CheckKeys(prompt);

            Apply(prompt);
        }

        // After the prompts, so the line matches this frame's text.
        UpdatePath();
    }

    void CheckKeys(Prompt prompt)
    {
        if (Input.GetButtonDown("Jump")) prompt.jumpPressed = true;
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) prompt.dashPressed = true;

        prompt.done |= prompt.keys switch
        {
            Keys.Move => Input.GetAxisRaw("Horizontal") != 0f,
            Keys.Jump => prompt.jumpPressed,
            Keys.Dash => prompt.dashPressed,
            Keys.JumpAndDash => prompt.jumpPressed && prompt.dashPressed,
            _ => false
        };
    }

    void Apply(Prompt prompt)
    {
        float fadeStep = Time.deltaTime / fadeSeconds;
        prompt.dim = Mathf.MoveTowards(prompt.dim, prompt.done ? 1f : 0f, fadeStep);

        if (prompt.done) prompt.sinceDone += Time.deltaTime;
        // A prompt with no keys is done once the player is through its stretch, so it goes straight away.
        float hideAfter = prompt.keys == Keys.None ? 0f : hideAfterSeconds;
        if (prompt.done && prompt.sinceDone >= hideAfter) prompt.hide = Mathf.MoveTowards(prompt.hide, 1f, fadeStep);

        if (prompt.hide >= 1f)
        {
            prompt.gone = true;
            prompt.text.enabled = false;
            return;
        }

        Color color = prompt.text.color;
        color.a = Mathf.Lerp(1f, dimmedAlpha, prompt.dim) * (1f - prompt.hide);
        prompt.text.color = color;
        prompt.text.transform.localScale = Vector3.one * Mathf.Lerp(1f, dimmedScale, prompt.dim);
    }

    // Retrying reloads the scene at a checkpoint; demos already watched this run don't play again.
    // Reaching the first prompt means the run started from the beginning, so all of them play again.
    // Their routes are kept, so after a retry the line still shows until the player completes it.
    static readonly HashSet<int> shownGhosts = new();
    static readonly Dictionary<int, Vector3[]> savedPaths = new();
    static readonly Dictionary<int, Vector3[]> savedDashes = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        shownGhosts.Clear();
        savedPaths.Clear();
        savedDashes.Clear();
    }

    void MaybePlayGhost(Prompt prompt)
    {
        int index = System.Array.IndexOf(prompts, prompt);
        if (index == 0)
        {
            shownGhosts.Clear();
            savedPaths.Clear();
            savedDashes.Clear();
        }
        if (prompt.ghostSteps.Length == 0 || demo != null) return;
        if (shownGhosts.Add(index))
        {
            demo = StartCoroutine(PlayGhost(prompt, index));
            return;
        }
        if (ghostPath && !prompt.hidePath && savedPaths.TryGetValue(index, out var points))
        {
            StartPath(prompt);
            pathPoints.AddRange(points);
            path.positionCount = pathPoints.Count;
            path.SetPositions(pathPoints.ToArray());
            if (savedDashes.TryGetValue(index, out var dashes))
                foreach (var dash in dashes) AddDashIcon(dash);
        }
    }

    // While the ghost runs, its position is added to the line. Afterwards the line stays until the
    // player reaches its end (or passes it), or the next prompt takes over; then it fades out.
    void UpdatePath()
    {
        if (path == null) return;

        if (ghost != null && demo != null)
        {
            Vector3 at = ghost.transform.position;
            if (pathPoints.Count == 0 || (at - pathPoints[^1]).sqrMagnitude > 0.0025f)
            {
                pathPoints.Add(at);
                path.positionCount = pathPoints.Count;
                path.SetPosition(pathPoints.Count - 1, at);
            }
            return;
        }

        // After the demo the line lasts exactly as long as its instruction's text: it fades out
        // with the text and is removed when the text is.
        if (pathPrompt == null || pathPrompt.gone || pathPrompt.text == null || !pathPrompt.text.enabled)
        {
            ClearPath();
            return;
        }
        Color color = ghostColor;
        color.a *= 1f - pathPrompt.hide;
        path.startColor = path.endColor = color;
        foreach (var icon in dashIcons)
            if (icon != null) icon.color = IconColor(1f - pathPrompt.hide);
    }

    void StartPath(Prompt prompt)
    {
        ClearPath();
        pathPrompt = prompt;
        var line = new GameObject("Ghost route");
        SceneManager.MoveGameObjectToScene(line, gameObject.scene);
        path = line.AddComponent<LineRenderer>();
        path.useWorldSpace = true;
        path.positionCount = 0;
        path.widthMultiplier = ghostPathWidth;
        path.numCapVertices = 4;
        path.numCornerVertices = 2;

        var body = player.GetComponentsInChildren<SpriteRenderer>(true).OrderBy(sprite => sprite.sortingOrder).FirstOrDefault();
        path.sharedMaterial = ghostPathMaterial != null ? ghostPathMaterial : body != null ? body.sharedMaterial : null;
        if (body != null)
        {
            path.sortingLayerID = body.sortingLayerID;
            path.sortingOrder = body.sortingOrder - 1;
        }
        path.startColor = path.endColor = ghostColor;
    }

    void ClearPath()
    {
        if (path != null) Destroy(path.gameObject);
        path = null;
        pathPrompt = null;
        pathPoints.Clear();
        pathDashes.Clear();
        dashIcons.Clear(); // children of the line, destroyed with it
    }

    // The icon is drawn fully opaque in the ghost's colour so it reads against the line.
    Color IconColor(float fade)
    {
        Color color = ghostColor;
        color.a = fade;
        return color;
    }

    void AddDashIcon(Vector3 mark)
    {
        if (path == null) return;
        pathDashes.Add(mark);

        var sprite = dashIcon != null ? dashIcon : DrawnDashIcon();
        var icon = new GameObject("Dash mark").AddComponent<SpriteRenderer>();
        icon.transform.SetParent(path.transform, false);
        icon.transform.position = new Vector3(mark.x, mark.y, 0f);
        float height = Mathf.Max(sprite.bounds.size.y, 0.0001f);
        icon.transform.localScale = Vector3.one * (dashIconSize / height);
        icon.sprite = sprite;
        icon.flipX = mark.z < 0f;
        icon.sharedMaterial = path.sharedMaterial;
        icon.sortingLayerID = path.sortingLayerID;
        icon.sortingOrder = path.sortingOrder + 1;
        icon.color = IconColor(1f);
        dashIcons.Add(icon);
    }

    // A white double chevron (>>) pointing right, made once in code so no image asset is needed.
    static Sprite DrawnDashIcon()
    {
        if (drawnDashIcon != null) return drawnDashIcon;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "Dash mark"
        };
        var pixels = new Color32[size * size];
        const float thickness = 0.085f;
        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
        {
            var p = new Vector2((px + 0.5f) / size, (py + 0.5f) / size);
            float d = float.MaxValue;
            foreach (float shift in new[] { 0f, 0.32f })
            {
                var tip = new Vector2(0.5f + shift, 0.5f);
                d = Mathf.Min(d, DistanceToSegment(p, new Vector2(0.18f + shift, 0.16f), tip));
                d = Mathf.Min(d, DistanceToSegment(p, new Vector2(0.18f + shift, 0.84f), tip));
            }
            float alpha = Mathf.Clamp01((thickness - d) * size * 0.5f + 0.5f);
            pixels[py * size + px] = new Color32(255, 255, 255, (byte)(alpha * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        drawnDashIcon = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        drawnDashIcon.name = "Dash mark";
        return drawnDashIcon;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    IEnumerator PlayGhost(Prompt prompt, int index)
    {
        // Wait out level loading and any open menu so the demo is seen from the start, and let the
        // player land: the dash prompt's gap is crossed mid-dash, and freezing there would strand them.
        var playerController = player.GetComponent<PlayerController>();
        yield return new WaitUntil(() => !GameMenuController.GameplayInputBlocked &&
                                         (playerController == null || (playerController.IsGrounded && !playerController.isDashing)));

        Hold();
        // The ghost appears on the player and runs from there. It heads for the point the route was
        // timed from and goes straight into the steps when it gets there, without stopping.
        ghost = MakeGhost(player.position);
        if (ghostPath && !prompt.hidePath) StartPath(prompt);
        var controller = ghost.GetComponent<PlayerController>();
        float total = prompt.ghostSteps.Sum(step => step.seconds);
        float start = float.PositiveInfinity;
        int lastStep = -1;

        if (cameraFollow != null)
        {
            cameraTarget = cameraFollow.player;
            cameraFollow.player = ghost.transform;
        }

        float approachUntil = Time.time + ghostApproachLimit;
        float side = Mathf.Sign(prompt.ghostStart.x - ghost.transform.position.x);
        controller.scriptedInput = () => new PlayerController.InputFrame { horizontal = side };
        while (Time.time < approachUntil)
        {
            float dx = prompt.ghostStart.x - ghost.transform.position.x;
            if (Mathf.Abs(dx) <= ghostStartTolerance || Mathf.Sign(dx) != side) break;
            yield return null;
        }

        start = Time.time;
        controller.scriptedInput = () =>
        {
            float t = Time.time - start;
            int index = 0;
            for (float end = 0f; index < prompt.ghostSteps.Length; index++)
            {
                end += prompt.ghostSteps[index].seconds;
                if (t < end) break;
            }
            if (index >= prompt.ghostSteps.Length) return default;

            var step = prompt.ghostSteps[index];
            bool begins = index != lastStep;
            lastStep = index;
            if (step.dash && begins && ghostPath && !prompt.hidePath)
            {
                float direction = step.move != 0f ? Mathf.Sign(step.move) : 1f;
                Vector3 at = ghost.transform.position;
                AddDashIcon(new Vector3(at.x, at.y, direction));
            }
            return new PlayerController.InputFrame
            {
                horizontal = step.move,
                jumpHeld = step.jump,
                jumpPressed = step.jump && begins,
                dashHeld = step.dash && begins
            };
        };

        // The player stays frozen until the ghost has played every step and come to rest on the ground
        // (a jump or dash can still be finishing when the last step ends), then for the end pause.
        var ghostBody = ghost.GetComponent<Rigidbody2D>();
        float giveUpAt = Time.time + total + ghostSettleLimit;
        while (Time.time < giveUpAt)
        {
            bool stepsDone = Time.time - start >= total;
            bool still = controller.IsGrounded && !controller.isDashing &&
                         (ghostBody == null || ghostBody.linearVelocity.sqrMagnitude < 0.01f);
            if (stepsDone && still) break;
            yield return null;
        }
        yield return new WaitForSeconds(ghostEndPause);

        UpdatePath();
        if (pathPrompt == prompt && pathPoints.Count > 1)
        {
            savedPaths[index] = pathPoints.ToArray();
            savedDashes[index] = pathDashes.ToArray();
        }
        EndGhost();
    }

    void Hold()
    {
        GameplayHold.Begin();
        holding = true;
        frozenBody = player.GetComponent<Rigidbody2D>();
        if (frozenBody == null) return;
        frozenVelocity = frozenBody.linearVelocity;
        frozenBody.simulated = false;
    }

    void EndGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;

        if (cameraFollow != null && cameraTarget != null) cameraFollow.player = cameraTarget;
        cameraTarget = null;

        if (frozenBody != null)
        {
            frozenBody.simulated = true;
            frozenBody.linearVelocity = frozenVelocity;
        }
        frozenBody = null;

        if (holding) GameplayHold.End();
        holding = false;
        demo = null;
    }

    void OnDisable()
    {
        if (demo != null) StopCoroutine(demo);
        EndGhost();
        ClearPath();
    }

    /// A copy of the player with only movement, light and physics left on it, untagged so nothing
    /// that looks for the player (checkpoints, coins, hazards, the door) reacts to it.
    GameObject MakeGhost(Vector2 at)
    {
        // Build it under an inactive parent so the removed parts never wake up.
        var holder = new GameObject("Ghost setup");
        holder.SetActive(false);
        var copy = Instantiate(player.gameObject, new Vector3(at.x, at.y, player.position.z), player.rotation, holder.transform);
        copy.name = "Tutorial Ghost";

        foreach (var child in copy.GetComponentsInChildren<Transform>(true).Skip(1).ToArray())
        {
            if (child == null) continue;
            bool lightsTheWorld = child.GetComponents<MonoBehaviour>().Any(b => b != null && (b.GetType().Name is "Light2D" or "LightSource"));
            if (lightsTheWorld) DestroyImmediate(child.gameObject);
        }

        var doomed = copy.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(b => b != null && !GhostKeeps.Contains(b.GetType().Name))
            .Cast<Component>()
            .ToList();
        // Remove what nothing else needs first, so RequireComponent never blocks a removal.
        for (int pass = 0; pass < 8 && doomed.Count > 0; pass++)
        {
            for (int i = doomed.Count - 1; i >= 0; i--)
            {
                if (IsRequired(doomed[i])) continue;
                DestroyImmediate(doomed[i]);
                doomed.RemoveAt(i);
            }
        }

        foreach (var t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.tag = "Untagged";

        foreach (var sprite in copy.GetComponentsInChildren<SpriteRenderer>(true))
            sprite.color *= ghostColor;

        // A copy made mid-dash would keep the dash flag without the timer that clears it, and ignore all input.
        var copyController = copy.GetComponent<PlayerController>();
        if (copyController != null) copyController.isDashing = false;

        // The player's body is frozen while the demo holds the game, and the copy inherits that.
        var rb = copy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        copy.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(copy, gameObject.scene);
        Destroy(holder);
        return copy;
    }

    static bool IsRequired(Component component)
    {
        foreach (var other in component.GetComponents<Component>())
        {
            if (other == null || other == component) continue;
            foreach (RequireComponent need in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                foreach (var type in new[] { need.m_Type0, need.m_Type1, need.m_Type2 })
                    if (type != null && type.IsInstanceOfType(component)) return true;
            }
        }
        return false;
    }
}
