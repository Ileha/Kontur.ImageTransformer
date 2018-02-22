using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer.Transform
{
    public unsafe class rotate_cw_builder : ABSBuilder {
        private byte* curpos;
        private int width;
        private int height;

        public rotate_cw_builder(Rectangle rect) : base(rect) {
            height = 0;
            width = BitmapData.Width-1;
            curpos = ((byte*)BitmapData.Scan0) + (height * BitmapData.Stride) + (width * 4);
        }
        public override void AddToData(byte* pos) {
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;
            *curpos = *pos; pos++; curpos++;

            height++;

            if (height >= BitmapData.Height) {
                height = 0;
                width--;
            }
            curpos = ((byte*)BitmapData.Scan0) + (height * BitmapData.Stride) + (width * 4);
        }
    }

    public class rotate_cw : ABSTransform {
        public override string Name { get { return "rotate_cw"; } }

        public override ABSBuilder GetBuilder(Rectangle rect) {
            return new rotate_cw_builder(rect);
        }

        public override Rectangle GetRectBeforeTransform(int x, int y, int w, int h, Bitmap picture)
        {
            int new_x = picture.Height - x;
            int new_y = y;
            new_x -= w;

            return new Rectangle(new_y, new_x, h, w);
            //X = x * cos(alpha) — y * sin(alpha);
            //Y = x * sin(alpha) + y * cos(alpha);
        }
    }
}
