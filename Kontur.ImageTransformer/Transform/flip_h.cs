using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer.Transform
{
    public unsafe class flip_h_builder : ABSBuilder {
        private byte* curpos;
        private int width;
        private int height;

        public flip_h_builder(Rectangle rect) : base(rect) {
            height = 0;
            width = BitmapData.Width-1;
            curpos = ((byte*)BitmapData.Scan0)+(width*4);
        }

        public override void AddToData(byte* pos) {
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;

            width--;

            if (width < 0){
                height++;
                width = BitmapData.Width - 1;                
            }
            curpos = ((byte*)BitmapData.Scan0) + (height * BitmapData.Stride) + (width * 4);
        }
    }

    public class flip_h : ABSTransform {
        public override string Name {
            get { return "flip-h"; }
        }

        public override ABSBuilder GetBuilder(Rectangle rect, Bitmap picture) {
            return new flip_h_builder(GetRectangleFromZero(rect, picture.Width, picture.Height));
        }

        public override Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture) {
            Rectangle into = GetRectangleFromZero(x, y, w, h, picture.Width, picture.Height);

            int new_x = into.X - picture.Width;
            new_x += into.Width;
            new_x *= -1;
            int new_y = into.Y;

            //Console.WriteLine("x={0}; y={1}; w={2}; h={3}", new_x, new_y, into.Width, into.Height);

            return new Rectangle(new_x, new_y, into.Width, into.Height);
        }
    }
}
