using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Transform
{
    public abstract class ABSTransform {
        public abstract string Name { get; }

        public abstract void GetRectBeforeTransform(ref int x, ref int y, ref int w, ref int h, Bitmap picture);
    }
}
