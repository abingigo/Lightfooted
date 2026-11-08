using TMPro;
using UnityEngine;

/// Reveals each key prompt when the player reaches its stretch of the level. Only that prompt
/// is on screen; it fades once its keys are pressed, stays faded for a few seconds, then
/// disappears for good. Reaching the next prompt removes the previous one straight away.
public class TutorialPrompts : MonoBehaviour
{
    enum Keys
    {
        Move,
        Jump,
        Dash,
        JumpAndDash
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

            prompt.reached |= current;

            // Respawning past a prompt that hasn't been seen on this load: nothing to show.
            if (!prompt.reached) prompt.gone = x >= prompt.endX;
            prompt.text.enabled = prompt.reached;
            if (!prompt.reached) continue;

            if (!current) prompt.done = true;
            else if (!inputBlocked) CheckKeys(prompt);

            Apply(prompt);
        }
    }

    void CheckKeys(Prompt prompt)
    {
        if (Input.GetButtonDown("Jump")) prompt.jumpPressed = true;
        if (Input.GetKeyDown(KeyCode.LeftShift)) prompt.dashPressed = true;

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
        if (prompt.sinceDone >= hideAfterSeconds) prompt.hide = Mathf.MoveTowards(prompt.hide, 1f, fadeStep);

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
}
