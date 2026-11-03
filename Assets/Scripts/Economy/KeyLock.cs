using Collectables;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Economy
{
    public sealed class KeyLock : CheckpointState
    {
        private const string Unlocked = "unlocked";

        [Tooltip("The key currency this lock needs.")]
        [SerializeField] private CurrencyType key;

        [Tooltip("Keys needed to open. 0 = every key of this type placed in the level.")]
        [SerializeField, Min(0)] private int required;

        [Tooltip("Spend the keys when the lock opens.")]
        [SerializeField] private bool consumeKeys;

        [Tooltip("Components kept disabled while locked, e.g. the door's trigger collider.")]
        [SerializeField] private Behaviour[] lockedComponents = { };

        [Tooltip("Objects kept inactive while locked.")]
        [SerializeField] private GameObject[] lockedObjects = { };

        [Tooltip("Optional label showing \"Keys x/N\" while locked.")]
        [SerializeField] private TMP_Text label;

        [SerializeField] private UnityEvent onUnlocked = new();

        private ICurrencyService currencies;
        private bool unlocked;
        private int needed;

        public bool IsUnlocked => unlocked;

        private void Awake() => SetOpen(false);

        private void Start()
        {
            if (key == null) Debug.LogWarning($"{name}: KeyLock has no key type; it opens at once.", this);
            needed = required > 0 ? required : KeysPlacedInLevel();
            if (key != null && needed == 0) Debug.LogWarning($"{name}: no {key.DisplayName} keys are placed in this level; the lock opens at once.", this);
            if (ServiceRegistry.TryGet(out currencies)) currencies.BalanceChanged += OnBalanceChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (currencies != null) currencies.BalanceChanged -= OnBalanceChanged;
        }

        public override string Capture() => unlocked ? Unlocked : string.Empty;

        public override void Restore(string data)
        {
            if (data == Unlocked) Open(false);
        }

        public override void RestoreConsumed() { }

        private void OnBalanceChanged(CurrencyType currency, int balance)
        {
            if (currency == key) Refresh();
        }

        private void Refresh()
        {
            if (unlocked) return;
            int have = currencies != null ? currencies.GetBalance(key) : 0;
            if (label != null) label.text = $"Keys {Mathf.Min(have, needed)}/{needed}";
            if (have >= needed) Open(consumeKeys);
        }

        private void Open(bool spend)
        {
            if (unlocked) return;
            unlocked = true;
            if (spend && currencies != null && needed > 0) currencies.TrySpend(key, needed);
            SetOpen(true);
            if (label != null) label.gameObject.SetActive(false);
            onUnlocked.Invoke();
        }

        private void SetOpen(bool open)
        {
            foreach (var component in lockedComponents)
                if (component != null) component.enabled = open;
            foreach (var target in lockedObjects)
                if (target != null) target.SetActive(open);
        }

        private int KeysPlacedInLevel()
        {
            int total = 0;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var collectable in root.GetComponentsInChildren<Collectable>(true))
                if (collectable.Definition is CurrencyCollectDef def && def.Currency == key)
                    total += def.Amount;
            return total;
        }
    }
}
