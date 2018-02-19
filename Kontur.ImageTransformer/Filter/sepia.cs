using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Filter
{
    class sepia : ABSFilter
    {
        public override string name
        {
            get { return "sepia"; }
        }

        //R = (oldR * .393) + (oldG * .769) + (oldB * .189) 
        //G = (oldR * .349) + (oldG * .686) + (oldB * .168) 
        //B = (oldR * .272) + (oldG * .534) + (oldB * .131) 

        public override void SetColor(ref byte a, ref byte r, ref byte g, ref byte b, int parameter)
        {
            if (a == 0) { return; }
            float _r = Convert.ToSingle(r);
            float _g = Convert.ToSingle(g);
            float _b = Convert.ToSingle(b);

            r = Convert.ToByte(Range((_r * 0.393f) + (_g * 0.769f) + (_b * 0.189f)));
            g = Convert.ToByte(Range((_r * 0.349f) + (_g * 0.686f) + (_b * 0.168f)));
            b = Convert.ToByte(Range((_r * 0.272f) + (_g * 0.534f) + (_b * 0.131f)));
        }

        private float Range(float num) {
            if (num > 255) { return 255; }
            else { return num; }
        }
    }
}
