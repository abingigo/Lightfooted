namespace Lightfooted.Analytics
{
    public interface IEventQueue
    {
        int Count { get; }
        string LastError { get; }
        void Enqueue(AnalyticsEvent data);
        bool Persist();
        AnalyticsEvent[] Peek(int limit = 32);
        bool Acknowledge(string[] ids);
    }
}
