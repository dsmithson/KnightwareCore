using Knightware.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    /// <summary>
    /// Worker class wrapped around an AsyncAutoResetEvent which provisions a worker thread to run when the AutoResetEvent is signalled
    /// </summary>
    public class AutoResetWorker
    {
        private AsyncAutoResetEvent asyncResetEvent;
        private bool isWorkerRunning;
        private object workerState;

        public bool IsRunning { get; private set; }
        
        /// <summary>
        /// When set, will automatically trigger the update method at a defined interval if not triggered manually.  Set to Timeout.InfiniteTimeSpan to disable.
        /// </summary>
        public TimeSpan PeriodicSignallingTime
        {
            get { return periodicSignallingTime; }
            set
            {
                periodicSignallingTime = value;
            }
        }
        private TimeSpan periodicSignallingTime = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// Executed before each iteration of the worker to determine if it should still be runing
        /// </summary>
        private Func<bool> checkForContinueMethod;

        /// <summary>
        /// Worker method run upon each iteration
        /// </summary>
        private Func<object, Task> workerMethod;

        public async Task<bool> StartupAsync(Func<object, Task> workerMethod, object workerState, Func<bool> checkForContinueMethod = null)
        {
            await ShutdownAsync();

            if (workerMethod == null)
                throw new ArgumentException("WorkerMethod cannot be null", "workerMethod");

            IsRunning = true;

            this.workerMethod = workerMethod;
            this.workerState = workerState;
            this.checkForContinueMethod = checkForContinueMethod;

            asyncResetEvent = new AsyncAutoResetEvent();
            await Task.Factory.StartNew(() => AsyncWorker(), TaskCreationOptions.LongRunning);
            return true;
        }

        /// <summary>
        /// Shuts down the auto reset worker.
        /// </summary>
        /// <param name="maxWait">Amount of time, in milliseconds, to wait for a current running task to complete.</param>
        /// <returns>True if shutdown was successful, or false if maxWait elapsed.  A return of false indicates the worker task has not yet completed.</returns>
        public async Task<bool> ShutdownAsync(int maxWait = Timeout.Infinite)
        {
            IsRunning = false;

            bool timedOut = false;
            if (isWorkerRunning)
            {
                if (asyncResetEvent != null)
                {
                    asyncResetEvent.Set();
                }

                DateTime starttime = DateTime.Now;
                while (isWorkerRunning)
                {
                    await Task.Delay(10);

                    if (maxWait != Timeout.Infinite && starttime.AddMilliseconds(maxWait) < DateTime.Now)
                    {
                        timedOut = true;
                        break;
                    }
                }
            }

            workerMethod = null;
            checkForContinueMethod = null;
            asyncResetEvent = null;

            return !timedOut;
        }

        /// <summary>
        /// Signals the AutoResetEvent
        /// </summary>
        public void Set()
        {
            if (IsRunning)
            {
                if(asyncResetEvent != null)
                    asyncResetEvent.Set();
            }
        }

        private async void AsyncWorker()
        {
            isWorkerRunning = true;

            while (IsRunning)
            {
                //Wait to be signalled
                await asyncResetEvent.WaitAsync(PeriodicSignallingTime);

                //Ensure we should still be running
                if (!IsRunning)
                    break;

                //Check if we should be running now.  If so, we wait, but don't break out of the loop
                if (checkForContinueMethod != null && !checkForContinueMethod())
                    continue;

                try
                {
                    await workerMethod(workerState);
                }
                catch (Exception ex)
                {
                    TraceQueue.Trace(this, TracingLevel.Warning, "{0} occurred while running worker method: {1}\r\n\r\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
                }
            }

            //Signal completion
            isWorkerRunning = false;
        }
    }
}
