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
using Kontur.ImageTransformer.Filter;
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
        private Dictionary<string, ABSFilter> Filters;
        private ConcurrentQueue<client> clients;
        private int prosess_count;
        public AsyncHttpServer()
        {
            listener = new HttpListener();
            request_parser = new Regex("process/(?<method>[\\w\\d()]+)/(?<rectangle>[\\d-+,]+)");
            Filters = new Dictionary<string, ABSFilter>();
            clients = new ConcurrentQueue<client>();
            prosess_count = Environment.ProcessorCount - 1;
        }

        public void Start(string prefix, params ABSFilter[] filters)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    foreach (ABSFilter f in filters) {
                        Filters.Add(f.name, f);
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
                        //Console.WriteLine("have empty prosses: {0}", prosess_count);
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
                int param = 0;
                ParseURL(listenerContext.Request.RawUrl, out method, ref digits, ref param);
                if (DateTime.Now.Ticks - time > 8500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                using (Bitmap b = new Bitmap(listenerContext.Request.InputStream)) {
                    if (DateTime.Now.Ticks - time > 9000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                    result = CropAndSetFilter(b, GetRectangleFromZero(digits[0], digits[1], digits[2], digits[3], b), GetFilter(method), param);
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

        private ABSFilter GetFilter(string name) {
            try {
                return Filters[name];
            }
            catch (KeyNotFoundException err) {
                throw new StatusCode(HttpStatusCode.BadRequest, true);
            }
        }

        private void ParseURL(string URL, out string method, ref int[] selection, ref int param)
        {
            Match m = request_parser.Match(URL);
            if (!m.Success) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
            method = m.Groups["method"].Value;
            int sk = method.IndexOf('(');
            if (sk != -1) {
                try { 
                    param = Convert.ToInt32(method.Substring(sk+1, method.Length-sk - 2));
                }
                catch (Exception err) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                method = method.Substring(0,sk);
            }
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
        private Rectangle GetRectangleFromZero(int x, int y, int w, int h, Bitmap image)
        {
            int new_x = 0, new_y = 0, new_w = 0, new_h = 0;

            if (w <= 0 || h <= 0) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            if (Math.Abs(x) >= image.Width || Math.Abs(y) >= image.Height) { throw new StatusCode(HttpStatusCode.NoContent, true); }

            if (x < 0) { new_x = 0; new_w = w + x; }
            else if (x > image.Width) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            else { new_x = x; new_w = w; }
            if (new_x + new_w > image.Width) { new_w = image.Width - new_x; }

            if (y < 0) { new_y = 0; new_h = h + y; }
            else if (y > image.Height) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            else { new_y = y; new_h = h; }
            if (new_y + new_h > image.Height) { new_h = image.Height - new_y; }

            if (new_w <= 0 || new_h <= 0) { throw new StatusCode(HttpStatusCode.NoContent, true); }

            return new Rectangle(new_x, new_y, new_w, new_h);
        }

        public unsafe Bitmap CropAndSetFilter(Bitmap png, Rectangle selection, ABSFilter Filter, int param)
        {
            Bitmap result = new Bitmap(selection.Width, selection.Height, PixelFormat.Format32bppArgb);
            BitmapData bd_new = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            BitmapData bd_old = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;
                byte* new_pos;

                byte* a, r, g, b;
                for (int h = selection.Y; h < selection.Height+selection.Y; h++)
                {
                    curpos = ((byte*)bd_old.Scan0) + (h * bd_old.Stride) + (selection.X*4);
                    new_pos = ((byte*)bd_new.Scan0) + ((h - selection.Y) * bd_new.Stride);
                    for (int w = 0; w < selection.Width; w++)
                    {
                        *new_pos = *curpos; b = new_pos; new_pos++; curpos++;
                        *new_pos = *curpos; g = new_pos; new_pos++; curpos++;
                        *new_pos = *curpos; r = new_pos; new_pos++; curpos++;
                        *new_pos = *curpos; a = new_pos; new_pos++; curpos++;

                        Filter.SetColor(ref *a, ref *r, ref *g, ref *b, param);
                    }
                }
            }
            finally {
                png.UnlockBits(bd_old);
                png.Dispose();
                result.UnlockBits(bd_new);
            }
            return result;
        }
    }
}