using UnityEngine;

namespace Core
{
    /// Holds gameplay without opening the pause menu: player input is ignored and the attempt timer
    /// stops, but time keeps running so something can play out meanwhile (the tutorial's ghost demo).
    public static class GameplayHold
    {
        private static int holds;

        public static bool Active => holds > 0;

        public static void Begin() => holds++;

        public static void End() => holds = Mathf.Max(0, holds - 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => holds = 0;
    }
}
