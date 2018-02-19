using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    public class client
    {
        public HttpListenerContext listenerContext;
        public long time;

        public client(HttpListenerContext listenerContext, long time) {
            this.listenerContext = listenerContext;
            this.time = time;
        }
    }
}
