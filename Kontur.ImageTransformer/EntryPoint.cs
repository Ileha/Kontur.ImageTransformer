using System;
using Kontur.ImageTransformer.Filter;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args) {
            using (var server = new AsyncHttpServer())
            {
                server.Start("http://+:8080/", new grayscale(), new sepia(), new threshold());
                Console.ReadKey(true);
            }
        }
    }
}
