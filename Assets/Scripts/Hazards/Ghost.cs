using Lighting;
using UnityEngine;

namespace Hazards
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Ghost : MonoBehaviour
    {
        enum State { Patrol, Notice, Chase, Return, Stone }

        [Header("Parts")]
        [Tooltip("Decides when the ghost is in light. Any light turns it to stone.")]
        [SerializeField] LightExposure exposure;

        [Tooltip("Solid collider used while stone. On the Ground layer so the player can stand and jump on it.")]
        [SerializeField] Collider2D stoneBody;

        [Tooltip("Holds the sprite. Shaken before the ghost wakes.")]
        [SerializeField] Transform visuals;

        [SerializeField] SpriteRenderer sprite;

        [Tooltip("Shown while the ghost has just noticed the player.")]
        [SerializeField] GameObject alert;

        [Header("Patrol")]
        [Tooltip("How far it drifts left and right of where it was placed.")]
        [SerializeField, Min(0f)] float patrolDistance = 3f;
        [SerializeField, Min(0f)] float patrolSpeed = 1.5f;

        [Header("Chase")]
        [SerializeField, Min(0f)] float noticeRange = 6f;
        [Tooltip("Seconds it pauses with the alert before chasing.")]
        [SerializeField, Min(0f)] float noticeTime = 0.5f;
        [SerializeField, Min(0f)] float chaseSpeed = 3f;
        [Tooltip("It gives up once the player stays farther than this for Lose Time.")]
        [SerializeField, Min(0f)] float loseRange = 9f;
        [SerializeField, Min(0f)] float loseTime = 2f;

        [Header("Stone")]
        [SerializeField] float stoneGravity = 5f;
        [Tooltip("Higher is harder for the player to push.")]
        [SerializeField, Min(0.01f)] float stoneMass = 2.5f;
        [Tooltip("Seconds it stays stone after it was last in light.")]
        [SerializeField, Min(0f)] float wakeDelay = 3f;
        [Tooltip("Seconds of shaking before it wakes.")]
        [SerializeField, Min(0f)] float shakeTime = 1f;
        [SerializeField, Min(0f)] float shakeAmount = 0.06f;

        [Header("Look")]
        [SerializeField] Color chaseTint = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] Color stoneTint = new(0.55f, 0.55f, 0.55f, 1f);

        Rigidbody2D body;
        Transform player;
        Vector2 home;
        Color baseColor;
        State state;
        float timer;
        float lostFor;
        float patrolSide = 1f;
        int lightLayer;

        public bool IsStone => state == State.Stone;

        void Reset()
        {
            exposure = GetComponentInChildren<LightExposure>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) visuals = sprite.transform;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            home = body.position;
            lightLayer = LayerMask.NameToLayer(LightSource.LayerName);
            if (sprite != null) baseColor = sprite.color;
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
            else Debug.LogWarning($"{name}: Ghost found no object tagged Player.", this);
            if (exposure == null) Debug.LogWarning($"{name}: Ghost has no LightExposure, so light can't stop it.", this);
            Float();
            Enter(State.Patrol);
        }

        void FixedUpdate()
        {
            if (exposure != null && exposure.IsExposed)
            {
                if (state != State.Stone) TurnToStone();
                timer = wakeDelay;
            }

            float step = Time.fixedDeltaTime;
            switch (state)
            {
                case State.Stone:
                    timer -= step;
                    if (visuals != null)
                        visuals.localPosition = timer < shakeTime ? (Vector3)(Random.insideUnitCircle * shakeAmount) : Vector3.zero;
                    if (timer <= 0f) Wake();
                    break;
                case State.Patrol:
                    var target = home + Vector2.right * (patrolSide * patrolDistance);
                    if (MoveToward(target, patrolSpeed)) patrolSide = -patrolSide;
                    if (Sees(noticeRange)) Enter(State.Notice);
                    break;
                case State.Notice:
                    timer -= step;
                    if (timer <= 0f) Enter(State.Chase);
                    break;
                case State.Chase:
                    if (player != null) MoveToward(player.position, chaseSpeed);
                    lostFor = Sees(loseRange) ? 0f : lostFor + step;
                    if (lostFor >= loseTime) Enter(State.Return);
                    break;
                case State.Return:
                    if (MoveToward(home, patrolSpeed)) Enter(State.Patrol);
                    else if (Sees(noticeRange)) Enter(State.Notice);
                    break;
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (state == State.Stone || other.gameObject.layer == lightLayer) return;
            var hit = other.attachedRigidbody;
            if (hit == null || !hit.CompareTag("Player")) return;
            LightPlatformer.UI.GameMenuController.NotifyGameOver("ghost");
        }

        void Enter(State next)
        {
            state = next;
            lostFor = 0f;
            if (next == State.Notice) timer = noticeTime;
            if (alert != null) alert.SetActive(next == State.Notice);
            if (sprite != null)
                sprite.color = next == State.Stone ? baseColor * stoneTint
                    : next == State.Chase ? baseColor * chaseTint
                    : baseColor;
        }

        void TurnToStone()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = stoneGravity;
            body.mass = stoneMass;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.linearVelocity = Vector2.zero;
            if (stoneBody != null) stoneBody.enabled = true;
            Enter(State.Stone);
        }

        void Wake()
        {
            Float();
            if (visuals != null) visuals.localPosition = Vector3.zero;
            Enter(Sees(loseRange) ? State.Chase : State.Return);
        }

        void Float()
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            if (stoneBody != null) stoneBody.enabled = false;
        }

        bool MoveToward(Vector2 target, float speed)
        {
            Vector2 offset = target - body.position;
            float reach = speed * Time.fixedDeltaTime;
            if (sprite != null && Mathf.Abs(offset.x) > 0.01f) sprite.flipX = offset.x < 0f;
            if (offset.sqrMagnitude <= reach * reach)
            {
                body.MovePosition(target);
                return true;
            }
            body.MovePosition(body.position + offset.normalized * reach);
            return false;
        }

        bool Sees(float range) =>
            player != null && ((Vector2)player.position - body.position).sqrMagnitude <= range * range;

        void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? (Vector3)home : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin + Vector3.left * patrolDistance, origin + Vector3.right * patrolDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, noticeRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, loseRange);
        }
    }
}
