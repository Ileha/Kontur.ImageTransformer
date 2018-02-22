﻿using System;
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
using System.Text;
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
            prosess_count = Environment.ProcessorCount - 1;
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
            //string Debug_result = "";

            try
            {
                //if (DateTime.Now.Ticks - time > 8000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                if (listenerContext.Request.HttpMethod != "POST") { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                if (listenerContext.Request.ContentLength64 > 102400) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                string method;
                int[] digits = new int[4];
                ParseURL(listenerContext.Request.RawUrl, out method, ref digits);
                //if (DateTime.Now.Ticks - time > 8500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                using (Bitmap b = new Bitmap(listenerContext.Request.InputStream)) {
                    if (b.Height * b.Width > 1000000) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                    //if (DateTime.Now.Ticks - time > 9000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                    
                    ABSTransform transform = GetFilter(method);
                    Rectangle before_transf = transform.GetRectBeforeTransform(digits[0], digits[1], digits[2], digits[3], b);
                    before_transf = GetRectangleFromZero(before_transf, b.Width, b.Height);
                    Rectangle after_transf = GetRectangleFromZero(digits[0], digits[1], digits[2], digits[3], b.Height, b.Width);
                    result = CropAndSetFilter(b, before_transf, after_transf, transform);
                    //if (DateTime.Now.Ticks - time > 9500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
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
        private Rectangle GetRectangleFromZero(Rectangle rect, int Width, int Height) {
            return GetRectangleFromZero(rect.X, rect.Y, rect.Width, rect.Height, Width, Height);
        }
        private Rectangle GetRectangleFromZero(int x, int y, int w, int h, int Width, int Height)
        {
            int new_x = 0, new_y = 0, new_w = 0, new_h = 0;

            if (w <= 0 || h <= 0) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            if (Math.Abs(x) >= Width || Math.Abs(y) >= Height) { throw new StatusCode(HttpStatusCode.NoContent, true); }

            if (x < 0) { new_x = 0; new_w = w + x; }
            else if (x > Width) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            else { new_x = x; new_w = w; }
            if (new_x + new_w > Width) { new_w = Width - new_x; }

            if (y < 0) { new_y = 0; new_h = h + y; }
            else if (y > Height) { throw new StatusCode(HttpStatusCode.NoContent, true); }
            else { new_y = y; new_h = h; }
            if (new_y + new_h > Height) { new_h = Height - new_y; }

            if (new_w <= 0 || new_h <= 0) { throw new StatusCode(HttpStatusCode.NoContent, true); }

            return new Rectangle(new_x, new_y, new_w, new_h);
        }

        public unsafe Bitmap CropAndSetFilter(Bitmap png, Rectangle before, Rectangle after, ABSTransform transform)
        {
            ABSBuilder build = transform.GetBuilder(after);
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

            //Bitmap result = new Bitmap(selection.Width, selection.Height, PixelFormat.Format32bppArgb);
            //BitmapData bd_new = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            //BitmapData bd_old = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            //try
            //{
            //    byte* curpos;
            //    byte* new_pos;

            //    byte* a, r, g, b;
            //    for (int h = selection.Y; h < selection.Height + selection.Y; h++)
            //    {
            //        curpos = ((byte*)bd_old.Scan0) + (h * bd_old.Stride) + (selection.X * 4);
            //        new_pos = ((byte*)bd_new.Scan0) + ((h - selection.Y) * bd_new.Stride);
            //        for (int w = 0; w < selection.Width; w++)
            //        {
            //            *new_pos = *curpos; b = new_pos; new_pos++; curpos++;
            //            *new_pos = *curpos; g = new_pos; new_pos++; curpos++;
            //            *new_pos = *curpos; r = new_pos; new_pos++; curpos++;
            //            *new_pos = *curpos; a = new_pos; new_pos++; curpos++;

            //            Filter.SetColor(ref *a, ref *r, ref *g, ref *b);
            //        }
            //    }
            //}
            //finally
            //{
            //    png.UnlockBits(bd_old);
            //    png.Dispose();
            //    result.UnlockBits(bd_new);
            //}
            //return result;
        }
    }
}