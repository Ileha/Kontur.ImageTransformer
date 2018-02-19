using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Filter
{
    class threshold : ABSFilter
    {
        public override string name
        {
            get { return "threshold"; }
        }
        /*
        intensity = (oldR + oldG + oldB) / 3 
 
        if intensity >= 255 * x / 100 
            R = 255 
            G = 255 
            B = 255 
        else 
            R = 0 
            G = 0 
            B = 0 
        */
        public override void SetColor(ref byte a, ref byte r, ref byte g, ref byte b, int parameter)
        {
            if (a == 0) { return; }
            float intensity = (Convert.ToSingle(r) + Convert.ToSingle(r) + Convert.ToSingle(r)) * 0.3333333333f;
            if (intensity >= 255 * parameter / 100f) {
                r = 255;
                g = 255;
                b = 255;
            }
            else {
                r = 0;
                g = 0;
                b = 0;
            }
        }
    }
}
