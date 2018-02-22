using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing;

namespace Kontur.ImageTransformer.Transform
{
    public unsafe class rotate_ccw_builder : ABSBuilder {
        private byte* curpos;
        private int width;
        private int height;

        public rotate_ccw_builder(Rectangle rect) : base(rect) {
            height = BitmapData.Height-1;
            width = 0;
            curpos = ((byte*)BitmapData.Scan0) + (height * BitmapData.Stride);
        }

        public override void AddToData(byte* pos) {
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;

            height--;

            if (height < 0) {
                width++;
                height = BitmapData.Height - 1;
            }
            curpos = ((byte*)BitmapData.Scan0) + (height * BitmapData.Stride) + (width * 4);
        }
    }

    public class rotate_ccw : ABSTransform
    {
        public override string Name { get { return "rotate-ccw"; } }

        public override ABSBuilder GetBuilder(Rectangle rect)
        {
            return new rotate_ccw_builder(rect);
        }

        public override Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture) {
            int new_y = x;
            int new_x = picture.Height - y;
            new_x -= h;

            Console.WriteLine("x={0}; y={1}; w={2}; h={3}", new_x, new_y, h, w);

            return new Rectangle(new_x, new_y, h, w);
        }
    }
}
