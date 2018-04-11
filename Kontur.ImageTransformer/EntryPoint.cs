using System;
using Kontur.ImageTransformer.Transform;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args) {
            using (var server = new AsyncHttpServer())
            {
                server.Start("http://+:10100/", new rotate_cw(), new rotate_ccw(), new flip_v(), new flip_h());
                Console.ReadKey(true);
            }
        }
    }
}
