using System;
using System.Threading;
using System.Threading.Tasks;

namespace MP.Scraping.Common.Tasks
{
    public class TaskQueue : IDisposable
    {
        public TaskQueue Next { get; private set; }
        public Task Task { get; private set; }

        public bool HasNextTask { get { return Next != null; } }

        private TaskQueue _previous;
        private readonly object _locker;

        public TaskQueue(Action action)
        {
            Task = new Task(action);
            _locker = new object();
        }
        public TaskQueue(Task task)
        {
            Task = task;
            _locker = new object();
        }
        private TaskQueue(Task task, TaskQueue previous, object locker)
        {
            _previous = previous;
            Task = task;
            _locker = locker;
        }

        public TaskQueue Then(Action action, CancellationToken cancellationToken)
        {
            lock (_locker)
            {
                if (HasNextTask)
                    return Next.Then(action, cancellationToken);

                Task nextT = Task.ContinueWith(t =>
                {
                    action();
                    t.Dispose();
                    _previous = null;
                }, cancellationToken);
                TaskQueue nextTQ = new TaskQueue(nextT, this, _locker);
                Next = nextTQ;
                return nextTQ;
            }
        }

        public void WaitAllTasks()
        {
            Task lastTask = GetLastNonCanceledTask();

            if (lastTask.Status == TaskStatus.Canceled || lastTask.Status == TaskStatus.Faulted)
                return;

            lastTask.Wait();
        }

        private Task GetLastNonCanceledTask()
        {
            TaskQueue tQueue = this;
            if (tQueue.Task.Status == TaskStatus.Canceled)
                while (tQueue == null || tQueue.Task.Status == TaskStatus.Canceled)
                    tQueue = tQueue._previous;
            else
                while (tQueue.HasNextTask && tQueue.Task.Status != TaskStatus.Canceled)
                    tQueue = tQueue.Next;

            return tQueue.Task;
        }

        public static void EmptyAction() { }

        public void Dispose()
        {
            _previous?.Dispose();
            Task.Dispose();
        }
    }
}
