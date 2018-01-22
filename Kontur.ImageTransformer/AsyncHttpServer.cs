using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;
using Kontur.ImageTransformer.Exceptions;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        
        private Regex request_parser;
        public AsyncHttpServer()
        {
            listener = new HttpListener();
            request_parser = new Regex("process/(?<method>[\\w\\d()]+)/(?<rectangle>[\\d-+,]+)");
        }
        
        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
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
        
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    //else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            // TODO: implement request handling
            StatusCode Code = new StatusCode(HttpStatusCode.OK, false);
            try {
                if (listenerContext.Request.HttpMethod != "POST") { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                if (listenerContext.Request.ContentLength64 > 102400) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                string method;
                int[] digits = new int[4];
                ParseURL(listenerContext.Request.RawUrl, out method, ref digits);
                Bitmap b = new Bitmap(listenerContext.Request.InputStream);
                Bitmap crop_b = Crop(b, GetRectangleFromZero(digits[0], digits[1], digits[2], digits[3], b));
                //crop_b.Save("test.png");
            }
            catch (StatusCode code) {
                Code = code;
            }
            //b.Save("test.png");
            listenerContext.Response.StatusCode = Code.ToInt;
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
                writer.WriteLine("Hello, world!");
        }

        private void ParseURL(string URL, out string method, ref int[] selection) {
            Match m = request_parser.Match(URL);
            if (!m.Success) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
            method = m.Groups["method"].Value;
            string[] digits = Regex.Split(m.Groups["rectangle"].Value, ",");
            for (int i = 0; i < selection.Length; i++) {
                try { 
                    selection[i] = Convert.ToInt32(digits[i]);
                }
                catch (Exception err) {
                    throw new StatusCode(HttpStatusCode.BadRequest, true);
                }
            }
        }
        private Rectangle GetRectangleFromZero(int x, int y, int w, int h, Bitmap image) {
            int new_x = 0, new_y = 0, new_w = 0, new_h = 0;
            
            if (w <= 0 || h <= 0) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            
            if (x < 0) { new_x = 0; new_w = w + x; }
            else { new_x = x; new_w = w; }
            if (new_x+new_w > image.Width) { new_w = image.Width-new_x; }

            if (y < 0) { new_y = 0; new_h = h + y; }
            else { new_y = y; new_h = h; }
            if (new_y+new_h > image.Height) { new_h = image.Height-new_y; }

            return new Rectangle(new_x, new_y, new_w, new_h);
        }

        private Bitmap Crop(Bitmap image, Rectangle selection) {
            Bitmap cropBmp = image.Clone(selection, image.PixelFormat);
            image.Dispose();
            return cropBmp;
        }
    }
}