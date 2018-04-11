using System;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Kontur.ImageTransformer.Transform;
using System.Collections.Concurrent;
using Kontur.ImageTransformer.ForTask;

namespace Kontur.ImageTransformer
{
    public class AsyncHttpServer : IDisposable
    {
        private readonly HttpListener listener;
        private TaskWrapper[] Tasks;

        private Thread listenerThread;
        private bool disposed;
        public volatile bool isRunning;

        public ManualResetEvent stopper;
        public Regex request_parser;
        public Dictionary<string, ABSTransform> Filters;
        public ConcurrentStack<client> clients;
        private int prosess_count;

        public AsyncHttpServer()
        {
            stopper = new ManualResetEvent(false);
            listener = new HttpListener();
            request_parser = new Regex("process/(?<method>[\\w-]+)/(?<rectangle>[\\d-+,]+)");
            Filters = new Dictionary<string, ABSTransform>();
            clients = new ConcurrentStack<client>();
            prosess_count = Environment.ProcessorCount;
            Tasks = new TaskWrapper[prosess_count*2];
            for (int i = 0; i < Tasks.Length; i++) {
                Tasks[i] = new TaskRepo(this, i);
            }
        }

        public void Start(string prefix, params ABSTransform[] filters)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    foreach (ABSTransform f in filters) {
                        Filters.Add(f.Name, f);
                    }

                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
                stopper.Set();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        private void Listen() {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        //Console.WriteLine("have empty prosses: {0}", prosess_count);
                        clients.Push(new client(context, DateTime.Now.Ticks));
                        stopper.Set();
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error) {
                    // TODO: log errors
                }
            }
        }
    }
}