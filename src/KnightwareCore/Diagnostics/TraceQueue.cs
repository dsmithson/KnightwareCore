﻿using Knightware.Threading.Tasks;
using System.Threading.Tasks;

namespace Knightware.Diagnostics
{
    public delegate void TracingLevelChangedHandler(TracingLevel newTracingLevel);
    public delegate void TraceMessageHandler(TraceMessage message);

    public static class TraceQueue
    {
        private const int maxMessageQueueCount = 100;
        private static readonly AsyncListProcessor<TraceMessage> messageQueue;

        /// <summary>
        /// Event raised when a new Trace message is generated
        /// </summary>
        public static event TraceMessageHandler TraceMessageRaised;

        /// <summary>
        /// Event raised when the tracing level has changed
        /// </summary>
        public static event TracingLevelChangedHandler TracingLevelChanged;
        private static void OnTracingLevelChanged(TracingLevel newTracingLevel)
        {
            if (TracingLevelChanged != null)
                TracingLevelChanged(newTracingLevel);
        }

        private static TracingLevel tracingLevel = Diagnostics.TracingLevel.Success;
        public static TracingLevel TracingLevel
        {
            get { return tracingLevel; }
            set
            {
                if (tracingLevel != value)
                {
                    tracingLevel = value;
                    OnTracingLevelChanged(value);
                }
            }
        }

        static TraceQueue()
        {
            //Initialize our message queue
            messageQueue = new AsyncListProcessor<TraceMessage>(ProcessQueue, maxQueueCount: maxMessageQueueCount);
            Task t = messageQueue.StartupAsync();
        }

        #region Trace Overloads

        public static void Trace(TracingLevel level, string message, params object[] args)
        {
            Trace(null, level, message, args);
        }

        public static void Trace(object sender, TracingLevel level, string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }

            Trace(new TraceMessage()
            {
                Level = level,
                Sender = sender,
                Message = message,
            });
        }

        public static void Trace(TraceMessage message)
        {
            if (message != null && message.Level <= tracingLevel && TraceMessageRaised != null)
                messageQueue.Add(message);
        }

        #endregion

        private static Task ProcessQueue(AsyncListProcessorItemEventArgs<TraceMessage> args)
        {
            if (args?.Item != null)
            {
                TraceMessageRaised?.Invoke(args.Item);
            }
            return Task.FromResult(true);
        }
    }
}
