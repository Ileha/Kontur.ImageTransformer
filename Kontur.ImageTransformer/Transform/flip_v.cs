using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer.Transform
{
    public unsafe class flip_v_builder : ABSBuilder {
        private byte* curpos;
        private int width;
        private int height;
        private byte* start_pos;

        public flip_v_builder(Rectangle rect) : base(rect)
        {
            height = BitmapData.Height-1;
            width = 0;
            start_pos = ((byte*)BitmapData.Scan0);
            curpos = start_pos+(BitmapData.Stride * height);
        }

        public override void AddToData(byte* pos) {
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;

            width++;

            if (width >= BitmapData.Width) {
                height--;
                width = 0;
                curpos = start_pos + (height * BitmapData.Stride);
            }
        }
    }
    public class flip_v : ABSTransform
    {
        public override string Name {
            get { return "flip-v"; }
        }
        public override ABSBuilder GetBuilder(Rectangle rect, Bitmap picture) {
            return new flip_v_builder(GetRectangleFromZero(rect, picture.Width, picture.Height));
        }

        public override Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture) {
            Rectangle into = GetRectangleFromZero(x, y, w, h, picture.Width, picture.Height);

            int new_x = into.X;
            int new_y = into.Y - picture.Height;
            new_y += into.Height;
            new_y *= -1;


            //Console.WriteLine("x={0}; y={1}; w={2}; h={3}", new_x, new_y, into.Width, into.Height);

            return new Rectangle(new_x, new_y, into.Width, into.Height);
        }
    }
}
