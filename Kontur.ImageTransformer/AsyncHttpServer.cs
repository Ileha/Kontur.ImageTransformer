using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;
using Kontur.ImageTransformer.Exceptions;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Kontur.ImageTransformer.Transform;
using System.Collections.Concurrent;
//using System.Diagnostics;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;

        private Regex request_parser;
        private Dictionary<string, ABSTransform> Filters;
        private ConcurrentQueue<client> clients;
        private int prosess_count;

        public AsyncHttpServer()
        {
            listener = new HttpListener();
            request_parser = new Regex("process/(?<method>[\\w-]+)/(?<rectangle>[\\d-+,]+)");
            Filters = new Dictionary<string, ABSTransform>();
            clients = new ConcurrentQueue<client>();
            prosess_count = Environment.ProcessorCount;
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
                        Console.WriteLine("have empty prosses: {0}", prosess_count);

                        if (prosess_count > 0) {
                            Runner(context, DateTime.Now.Ticks);
                        }
                        else {
                            clients.Enqueue(new client(context, DateTime.Now.Ticks));
                        }
                    }
                    //else Thread.Sleep(0);
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

        private void Runner(HttpListenerContext listenerContext, long time) {
            prosess_count -= 1;
            Task.Run(() => {
                //Console.WriteLine("start handle");
                HandleContextAsync(listenerContext, time);
                client cli;
                while (true) {
                    if (clients.TryDequeue(out cli)) {
                        if (DateTime.Now.Ticks - cli.time > 9500000) {
                            //Console.WriteLine("have client bad");
                            cli.listenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            cli.listenerContext.Response.OutputStream.Dispose();
                        }
                        else {
                            //Console.WriteLine("have client good");
                            HandleContextAsync(cli.listenerContext, cli.time);
                        }
                    }
                    else {
                        prosess_count += 1;
                        break;
                    }
                }
            });
        }

        private void HandleContextAsync(HttpListenerContext listenerContext, long time)
        {
            // TODO: implement request handling

            StatusCode Code = new StatusCode(HttpStatusCode.OK, false);
            Bitmap result = null;
            try
            {
                if (DateTime.Now.Ticks - time > 8000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                if (listenerContext.Request.HttpMethod != "POST") { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                if (listenerContext.Request.ContentLength64 > 102400) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                string method;
                int[] digits = new int[4];
                ParseURL(listenerContext.Request.RawUrl, out method, ref digits);
                if (DateTime.Now.Ticks - time > 8500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                using (Bitmap b = new Bitmap(listenerContext.Request.InputStream)) {
                    if (b.Height * b.Width > 1000000) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                    if (DateTime.Now.Ticks - time > 9000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                    ABSTransform transform = GetFilter(method);
                    Rectangle before_transf = transform.GetRectBeforeTransform(digits[0], digits[1], digits[2], digits[3], b);
                    Rectangle after_transf = new Rectangle(digits[0], digits[1], digits[2], digits[3]);
                    result = CropAndSetFilter(b, before_transf, after_transf, transform);
                    if (DateTime.Now.Ticks - time > 9500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                }
            }
            catch (StatusCode code)
            {
                Code = code;
            }
            listenerContext.Response.StatusCode = Code.ToInt;
            if (Code.IsFatal) {
                listenerContext.Response.OutputStream.Dispose();
            }
            else
            {
                listenerContext.Response.ContentType = "image/png";
                result.Save(listenerContext.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png);
                listenerContext.Response.OutputStream.Dispose();
            }

            if (result != null) {
                result.Dispose();
            }
        }

        private ABSTransform GetFilter(string name) {
            try {
                return Filters[name];
            }
            catch (KeyNotFoundException err) {
                throw new StatusCode(HttpStatusCode.BadRequest, true);
            }
        }

        private void ParseURL(string URL, out string method, ref int[] selection)
        {
            Match m = request_parser.Match(URL);
            if (!m.Success) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
            method = m.Groups["method"].Value;
            string[] digits = Regex.Split(m.Groups["rectangle"].Value, ",");
            for (int i = 0; i < selection.Length; i++)
            {
                try
                {
                    selection[i] = Convert.ToInt32(digits[i]);
                }
                catch (Exception err)
                {
                    throw new StatusCode(HttpStatusCode.BadRequest, true);
                }
            }
        }

        public unsafe Bitmap CropAndSetFilter(Bitmap png, Rectangle before, Rectangle after, ABSTransform transform)
        {
            ABSBuilder build = transform.GetBuilder(after, png);
            BitmapData bd_old = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {
                byte* curpos;

                for (int h = before.Y; h < before.Height + before.Y; h++) {
                    curpos = ((byte*)bd_old.Scan0) + (h * bd_old.Stride) + (before.X * 4);
                    for (int w = 0; w < before.Width; w++) {
                        build.AddToData(curpos);
                        curpos+=4;
                    }
                }
            }
            finally {
                png.UnlockBits(bd_old);
                png.Dispose();
            }
            return build.Bitmap;
        }
    }
}