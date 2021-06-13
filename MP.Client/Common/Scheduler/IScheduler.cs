using System;

namespace MP.Client.Common.Scheduler
{
    public interface IScheduler
    {
        TimeSpan Interval { get; }
        DateTime StartTime { get; }

        Action CreateSchedulerAction();
    }
}
