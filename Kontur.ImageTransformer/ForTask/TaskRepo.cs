using Kontur.ImageTransformer.Exceptions;
using Kontur.ImageTransformer.Transform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;

namespace Kontur.ImageTransformer.ForTask
{
    public class TaskRepo : TaskWrapper {
        private client DataToHandle;

        protected override Action execute {
            get {
                return HandleContextAsync;
            }
        }

        public TaskRepo(AsyncHttpServer listner) : base(listner) {

        }

        public unsafe Bitmap CropAndSetFilter(Bitmap png, Rectangle before, Rectangle after, ABSTransform transform)
        {
            ABSBuilder build = transform.GetBuilder(after, png);
            BitmapData bd_old = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;

                for (int h = before.Y; h < before.Height + before.Y; h++)
                {
                    curpos = ((byte*)bd_old.Scan0) + (h * bd_old.Stride) + (before.X * 4);
                    for (int w = 0; w < before.Width; w++)
                    {
                        build.AddToData(curpos);
                        curpos += 4;
                    }
                }
            }
            finally
            {
                png.UnlockBits(bd_old);
                png.Dispose();
            }
            return build.Bitmap;
        }

        private ABSTransform GetFilter(string name)
        {
            try {
                return Data.Filters[name];
            }
            catch (KeyNotFoundException err)
            {
                throw new StatusCode(HttpStatusCode.BadRequest, true);
            }
        }

        private void ParseURL(string URL, out string method, ref int[] selection)
        {
            Match m = Data.request_parser.Match(URL);
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

        private void HandleContextAsync()
        {
            // TODO: implement request handling

            StatusCode Code = new StatusCode(HttpStatusCode.OK, false);
            Bitmap result = null;
            try
            {
                if (DateTime.Now.Ticks - DataToHandle.time > 8000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                if (DataToHandle.listenerContext.Request.HttpMethod != "POST") { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                if (DataToHandle.listenerContext.Request.ContentLength64 > 102400) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                string method;
                int[] digits = new int[4];
                ParseURL(DataToHandle.listenerContext.Request.RawUrl, out method, ref digits);
                if (DateTime.Now.Ticks - DataToHandle.time > 8500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                using (Bitmap input = new Bitmap(DataToHandle.listenerContext.Request.InputStream))
                {
                    if (input.Height * input.Width > 1000000) { throw new StatusCode(HttpStatusCode.BadRequest, true); }
                    if (DateTime.Now.Ticks - DataToHandle.time > 9000000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                    ABSTransform transform = GetFilter(method);
                    Rectangle before_transf = transform.GetRectBeforeTransform(digits[0], digits[1], digits[2], digits[3], input);
                    Rectangle after_transf = new Rectangle(digits[0], digits[1], digits[2], digits[3]);
                    result = CropAndSetFilter(input, before_transf, after_transf, transform);
                    if (DateTime.Now.Ticks - DataToHandle.time > 9500000) { throw new StatusCode(HttpStatusCode.InternalServerError, true); }
                }
            }
            catch (StatusCode code)
            {
                Code = code;
            }
            DataToHandle.listenerContext.Response.StatusCode = Code.ToInt;
            if (Code.IsFatal)
            {
                DataToHandle.listenerContext.Response.OutputStream.Dispose();
            }
            else
            {
                DataToHandle.listenerContext.Response.ContentType = "image/png";
                result.Save(DataToHandle.listenerContext.Response.OutputStream, ImageFormat.Png);
                DataToHandle.listenerContext.Response.OutputStream.Dispose();
            }

            if (result != null)
            {
                result.Dispose();
            }
        }

        protected override bool IsNextStep() {
            return Data.clients.TryPop(out DataToHandle);
        }
    }
}
