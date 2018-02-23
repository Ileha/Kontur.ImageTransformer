using System.Net;

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
