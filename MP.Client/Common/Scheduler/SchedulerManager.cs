using System;
using System.Collections.Generic;
using System.Threading;

namespace MP.Client.Common.Scheduler
{
    public class SchedulerManager
    {
        private static SchedulerManager _scheduleManager;
        
        private Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();

        public static SchedulerManager GetInstance()
        {
            if (_scheduleManager == null)
                _scheduleManager = new SchedulerManager();

            return _scheduleManager;
        }

        private SchedulerManager()
        { }

        public void AddTask(string taskName, IScheduler scheduler)
        {
            ThrowIfTaskExists(taskName);
            Action taskAction = scheduler.CreateSchedulerAction();
            AddTask(taskName, taskAction, scheduler.Interval, scheduler.StartTime);
        }

        public void AddTask(string taskName, Action task, TimeSpan interval, DateTime startTime)
        {
            ThrowIfTaskExists(taskName);

            while (startTime < DateTime.Now)
                startTime = startTime.AddMilliseconds(interval.TotalMilliseconds);

            TimerCallback timerCallback = i => task.Invoke();
            TimeSpan dueTime = startTime - DateTime.Now;
            Timer timer = new Timer(timerCallback, null, dueTime, interval);
            _timers.Add(taskName, timer);
        }

        public Timer GetTask(string taskName)
        {
            ThrowIfTaskAbsent(taskName);
            return _timers[taskName];
        }

        public void StopTask(string taskName)
        {
            Timer timer = GetTask(taskName);
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
            _timers.Remove(taskName);
        }

        public bool HasTask(string taskName) => _timers.ContainsKey(taskName);

        private void ThrowIfTaskAbsent(string taskName)
        {
            if (!HasTask(taskName))
                throw new KeyNotFoundException("Task was not found");
        }

        private void ThrowIfTaskExists(string taskName)
        {
            if (HasTask(taskName))
                throw new ArgumentException("Timer with same name already exists");
        }
    }
}
