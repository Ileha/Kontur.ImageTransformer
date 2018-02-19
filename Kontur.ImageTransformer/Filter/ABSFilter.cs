using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Filter
{
    public abstract class ABSFilter {
        public abstract string name { get; }

        public abstract void SetColor(ref byte a, ref byte r, ref byte g, ref byte b, int parameter);
    }
}
