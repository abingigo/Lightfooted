using Core;

namespace Collectables
{
    public sealed class ChallengeCoinRecordState : CheckpointState
    {
        public override string Capture() =>
            ServiceRegistry.TryGet(out IChallengeCoinService service) && service is ChallengeCoinRecord record
                ? record.CaptureAttempt()
                : string.Empty;

        public override void Restore(string data)
        {
            if (!string.IsNullOrEmpty(data) && ServiceRegistry.TryGet(out IChallengeCoinService service) &&
                service is ChallengeCoinRecord record)
                record.RestoreAttempt(data);
        }

        public override void RestoreConsumed() { }
    }
}
