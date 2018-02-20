using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kontur.ImageTransformer.Transform
{
    public class rotate_cw : ABSTransform {
        public override string Name { get { return "rotate_cw"; } }

        public override void GetRectBeforeTransform(ref int x, ref int y, ref int w, ref int h, Bitmap picture)
        {
            int new_x = picture.Height - x;
            int new_y = y;
            new_x -= w;
            y = new_x;
            x = new_y;

            int w_new = w;
            w = h;
            h = w_new;

            //X = x * cos(alpha) — y * sin(alpha);
            //Y = x * sin(alpha) + y * cos(alpha);
        }
    }
}
