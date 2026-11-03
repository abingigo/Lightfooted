using Core;

namespace Economy
{
    public sealed class CurrencyCheckpointState : CheckpointState
    {
        public override string Capture() =>
            ServiceRegistry.TryGet(out ICurrencyService currencies) ? currencies.CaptureLevelEarnings() : string.Empty;

        public override void Restore(string data)
        {
            if (!string.IsNullOrEmpty(data) && ServiceRegistry.TryGet(out ICurrencyService currencies))
                currencies.RestoreLevelEarnings(data);
        }

        public override void RestoreConsumed() { }
    }
}
