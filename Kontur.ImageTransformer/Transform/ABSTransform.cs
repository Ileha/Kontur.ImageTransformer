using Kontur.ImageTransformer.Exceptions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Transform
{
    public abstract class ABSTransform {
        public abstract string Name { get; }

        public abstract ABSBuilder GetBuilder(Rectangle rect, Bitmap picture);
        public abstract Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture);
        public Rectangle GetRectBeforeTransform(Rectangle rect, Bitmap picture) {
            return GetRectBeforeTransform(rect.X, rect.Y, rect.Width, rect.Height, picture);
        }

        public static Rectangle GetRectangleFromZero(Rectangle rect, int Width, int Height)
        {
            return GetRectangleFromZero(rect.X, rect.Y, rect.Width, rect.Height, Width, Height);
        }
        public static Rectangle GetRectangleFromZero(int x, int y, int w, int h, int Width, int Height)
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
    }

    public abstract unsafe class ABSBuilder {
        private Bitmap result;
        private BitmapData data;
        public BitmapData BitmapData { get { return data; } }
        public Bitmap Bitmap { 
            get {
                result.UnlockBits(data);
                return result;
            }
        }
        public ABSBuilder(Rectangle rect) {
            result = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        }
        public abstract void AddToData(byte* pos);
    }
}
