using System;
using System.Collections.Generic;
using System.Threading;

namespace MP.Scraping.Common.Tasks
{
    public class SchedulerService
    {
        private const int MS_IN_SECOND = 1000;
        private const int MS_IN_MINUTE = 1000 * 60;
        private const int MS_IN_HOUR = 1000 * 60 * 60;
        private const int MS_IN_DAY = 1000 * 60 * 60 * 24;

        private static Dictionary<string, Timer> _timers = new Dictionary<string, Timer>();
        
        public static Timer AddTask(string name, int interval, TimePeriod timePeriod, Action action) =>
            AddTask(name, interval, timePeriod, DateTime.Now, action);

        public static Timer AddTask(string name, int interval, TimePeriod timePeriod, DateTime startTime, Action action)
        {
            if (_timers.ContainsKey(name))
                throw new ArgumentException();

            TimerCallback tc = i => action.Invoke();
            int period = GetPeriod(interval, timePeriod);
            int startDelay = GetStartDelay(startTime, period);

            Timer timer = new Timer(tc, null, startDelay, period);

            _timers.Add(name, timer);

            return timer;
        }

        public static Timer GetTimer(string name)
        {
            if (!_timers.ContainsKey(name))
                return null;

            return _timers[name];
        }

        public static void StopAndRemoveTimer(string name)
        {
            if (!_timers.ContainsKey(name))
                throw new ArgumentException();

            _timers[name].Dispose();
            _timers.Remove(name);
        }

        //можно будет переписать на случай, если будет указано очень древне время
        private static int GetStartDelay(DateTime startTime, int period)
        {
            DateTime now = DateTime.Now;

            if (startTime <= now)
                while (startTime < now)
                    startTime = startTime.AddMilliseconds(period);

            return (int)((startTime - now).TotalMilliseconds);
        }

        private static int GetPeriod(int interval, TimePeriod timePeriod)
        {
            switch (timePeriod)
            {
                case TimePeriod.Seconds: return interval * MS_IN_SECOND;
                case TimePeriod.Minutes: return interval * MS_IN_MINUTE;
                case TimePeriod.Hours: return interval * MS_IN_HOUR;
                case TimePeriod.Days: return interval * MS_IN_DAY;
                default: return 0;
            }
                    
        }
    }

    public enum TimePeriod
    {
        Seconds,
        Minutes,
        Hours,
        Days
    }
}
