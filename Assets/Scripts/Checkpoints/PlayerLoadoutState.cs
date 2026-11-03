using System;
using Core;
using UnityEngine;

public class PlayerLoadoutState : CheckpointState
{
    [Serializable]
    struct Loadout
    {
        public bool lantern;
    }

    [Tooltip("The player's lantern. Restored at full time if it was lit at the checkpoint.")]
    [SerializeField] LanternTimer lantern;

    void Reset() => lantern = GetComponentInChildren<LanternTimer>(true);

    public override string Capture() =>
        JsonUtility.ToJson(new Loadout { lantern = lantern != null && lantern.gameObject.activeSelf });

    public override void Restore(string data)
    {
        var loadout = JsonUtility.FromJson<Loadout>(data);
        if (lantern == null) return;

        lantern.gameObject.SetActive(false);
        lantern.gameObject.SetActive(loadout.lantern);
        var tracker = GetComponent<PlayerLightTracker>();
        if (tracker != null) tracker.hasLantern = loadout.lantern;
    }

    public override void RestoreConsumed() { }
}
