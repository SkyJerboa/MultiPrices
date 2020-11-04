using System;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.Common.Tasks
{
    public class LinkedTask : IDisposable
    {
        public LinkedTask Previous { get; }
        public LinkedTask Next { get; private set; }
        public Task Task { get; }

        public bool HasNextTask { get { return Next != null; } }

        readonly object _locker;

        public LinkedTask(Action action)
        {
            Task = new Task(action);
            _locker = new object();
        }
        public LinkedTask(Task task)
        {
            Task = task;
            _locker = new object();
        }
        private LinkedTask(Task task, LinkedTask previous, object locker)
        {
            Previous = previous;
            Task = task;
            _locker = locker;
        }

        public LinkedTask Then(Action action, CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                if (HasNextTask)
                    return Next.Then(action, cancellationToken);

                Task nextT = Task.ContinueWith(t =>
                {
                    action();
                    t.Dispose();
                }, cancellationToken);
                LinkedTask nextLt = new LinkedTask(nextT, this, _locker);
                Next = nextLt;
                return nextLt;
            }
        }

        public void WaitAllTasks()
        {
            Task.Wait();
            if (HasNextTask)
                Next.WaitAllTasks();
        }

        public static void EmptyAction() { }

        public void Dispose()
        {
            Previous?.Dispose();
            Task.Dispose();
        }
    }
}
