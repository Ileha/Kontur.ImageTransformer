using System;
using Kontur.ImageTransformer.Transform;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args) {
            using (var server = new AsyncHttpServer())
            {
                server.Start("http://+:8080/", new rotate_cw());
                Console.ReadKey(true);
            }
        }
    }
}
