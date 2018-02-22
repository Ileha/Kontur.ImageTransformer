using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Transform
{
    public abstract class ABSTransform {
        public abstract string Name { get; }

        public abstract ABSBuilder GetBuilder(Rectangle rect);
        public abstract Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture);
        public Rectangle GetRectBeforeTransform(Rectangle rect, Bitmap picture) {
            return GetRectBeforeTransform(rect.X, rect.Y, rect.Width, rect.Height, picture);
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
