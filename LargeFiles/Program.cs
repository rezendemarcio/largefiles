using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LargeFiles
{
    /*
     * Implementação do código obtida do blog EximiaCo.Tech, em:
     * https://www.eximiaco.tech/pt/2020/06/18/fazendo-parsing-de-arquivos-grandes/
     * 
     * Crédito: Elemar Júnior
     */

    class Program
    {
        static string filePath = @"D:\dev\ml-25m\ratings.csv";

        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            var before2 = GC.CollectionCount(2);
            var before1 = GC.CollectionCount(1);
            var before0 = GC.CollectionCount(0);
            Impl_Stream();           // implementação
            sw.Stop();

            Console.WriteLine($"Tempo total: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"GC Gen #2 : {GC.CollectionCount(2) - before2}");
            Console.WriteLine($"GC Gen #1 : {GC.CollectionCount(1) - before1}");
            Console.WriteLine($"GC Gen #0 : {GC.CollectionCount(0) - before0}");
            Console.WriteLine($"Memory: {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB ");
            Console.WriteLine("\n\nPronto");
            Console.ReadKey();

            /*
            Impl_Naive();
            Impl_Stream();
            Impl_Span();
            Impl_Bytes();
            */
        }

        static void Impl_Naive()
        {
            var lines = File.ReadAllLines(filePath);
            var sum = 0d;
            var count = 0;

            foreach (var line in lines)
            {
                var parts = line.Split(',');

                if (parts[1] == "110")
                {
                    sum += double.Parse(parts[2], CultureInfo.InvariantCulture);
                    count++;
                }
            }

            Console.WriteLine($"Impl_Naive(): Classificação média para o filme Coração Valente é {sum / count} ({count}).");
        }

        static void Impl_Stream()
        {
            var sum = 0d;
            var count = 0;
            string line;

            using (var fs = File.OpenRead(filePath))
            using (var reader = new StreamReader(fs))
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');

                    if (parts[1] == "110")
                    {
                        sum += double.Parse(parts[2], CultureInfo.InvariantCulture);
                        count++;
                    }
                }
            Console.WriteLine($"Impl_Stream(): Classificação média para o filme Coração Valente é {sum / count} ({count}).");
        }

        static void Impl_Span()
        {
            var sum = 0d;
            var count = 0;
            string line;

            // ID do filme Coração Valente como um Span;
            var lookingFor = "110".AsSpan();

            using (var fs = File.OpenRead(filePath))
            using (var reader = new StreamReader(fs))
                while ((line = reader.ReadLine()) != null)
                {
                    // Ignorando o ID do usuário 
                    var span = line.AsSpan(line.IndexOf(',') + 1);

                    // ID do filme
                    var firstCommaPos = span.IndexOf(',');
                    var movieId = span.Slice(0, firstCommaPos);
                    if (!movieId.SequenceEqual(lookingFor)) continue;

                    // Classificação
                    span = span.Slice(firstCommaPos + 1);
                    firstCommaPos = span.IndexOf(',');
                    var rating = double.Parse(span.Slice(0, firstCommaPos).ToString(), provider: CultureInfo.InvariantCulture);

                    sum += rating;
                    count++;
                }

            Console.WriteLine($"Impl_Span(): Classificação média para o filme Coração Valente é {sum / count} ({count}).");
        }

        static void Impl_Bytes()
        {
            var sum = 0d;
            var count = 0;

            var lookingFor = Encoding.UTF8.GetBytes("110").AsSpan();
            var rawBuffer = new byte[1024 * 1024];
            using (var fs = File.OpenRead(filePath))
            {
                var bytesBuffered = 0;
                var bytesConsumed = 0;

                while (true)
                {
                    var bytesRead = fs.Read(rawBuffer, bytesBuffered, rawBuffer.Length - bytesBuffered);

                    if (bytesRead == 0) break;
                    bytesBuffered += bytesRead;

                    int linePosition;

                    do
                    {
                        linePosition = Array.IndexOf(rawBuffer, (byte)'\n', bytesConsumed,
                            bytesBuffered - bytesConsumed);

                        if (linePosition >= 0)
                        {
                            var lineLength = linePosition - bytesConsumed;
                            var line = new Span<byte>(rawBuffer, bytesConsumed, lineLength);
                            bytesConsumed += lineLength + 1;


                            // Ignorando o ID do usuário
                            var span = line.Slice(line.IndexOf((byte)',') + 1);

                            // ID do filme
                            var firstCommaPos = span.IndexOf((byte)',');
                            var movieId = span.Slice(0, firstCommaPos);
                            if (!movieId.SequenceEqual(lookingFor)) continue;

                            // Classíficação
                            span = span.Slice(firstCommaPos + 1);
                            firstCommaPos = span.IndexOf((byte)',');
                            var rating = double.Parse(Encoding.UTF8.GetString(span.Slice(0, firstCommaPos).ToArray()), provider: CultureInfo.InvariantCulture);

                            sum += rating;
                            count++;
                        }

                    } while (linePosition >= 0);

                    Array.Copy(rawBuffer, bytesConsumed, rawBuffer, 0, (bytesBuffered - bytesConsumed));
                    bytesBuffered -= bytesConsumed;
                    bytesConsumed = 0;
                }
            }
            Console.WriteLine($"Impl_Bytes(): Classificação média para o filme Coração Valente é {sum / count} ({count}).");
        }
    }
}