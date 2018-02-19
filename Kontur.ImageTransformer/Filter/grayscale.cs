using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Filter
{
    class grayscale : ABSFilter
    {
        public override string name
        {
            get { return "grayscale"; }
        }

        public override void SetColor(ref byte a, ref byte r, ref byte g, ref byte b, int parameter)
        {
            if (a == 0) { return; }
            short inten = (short)((Convert.ToInt16(r) + Convert.ToInt16(g) + Convert.ToInt16(b)) * 0.3333333333f);
            r = Convert.ToByte(inten);
            g = r;
            b = r;
        }
    }
}