using System.Collections.Concurrent;
using System.Numerics;

namespace BabeliaSharp
{
    internal class Program
    {
        //BigInteger needs System.Runtime.Numerics nuGet Package

        //3^2015755
        private static BigInteger c_m = BigInteger.Pow(new BigInteger(3), 2015755);

        //3^1007922+9^57319+1
        private static BigInteger c_a = BigInteger.Pow(new BigInteger(3), 1007922) + BigInteger.Pow(new BigInteger(9), 57319) + 1;

        //224737^91586
        private static BigInteger c_c = BigInteger.Pow(new BigInteger(224737), 91586);

        //2^2496009 - 1
        private static BigInteger c_maskone = BigInteger.Pow(new BigInteger(2), 2496009) - 1;

        //2^1697289 - 1
        private static BigInteger c_masktwo = BigInteger.Pow(new BigInteger(2), 1697289) - 1;

        //2^399361
        private static BigInteger c_divver = BigInteger.Pow(new BigInteger(2), 399361);



        //Config
        private static string fName = "F:\\babellog.log"; //Log File
        private static UInt64 THREADS = (UInt64)Environment.ProcessorCount;
        private static UInt64 BATCH = 4;


        static void Main(string[] args)
        {
            Console.WriteLine("*********Beginning Scan for DOTS*********\r\n\r\n");
            DateTime lastPrint = DateTime.Now;
            bool doPrint = false;

            Random r = new Random();
            
            List<Tuple<UInt64, int>> foundList = new List<Tuple<ulong, int>>();
            UInt64 scanTotal = 0;

            DateTime startTime = DateTime.Now;
            UInt64 index = 0;

            while (true)
            {
                doPrint = false;
                if((DateTime.Now - lastPrint).TotalSeconds > 5)
                {
                    lastPrint = DateTime.Now;
                    doPrint = true;
                }

                byte[] rdm = new byte[8];
                r.NextBytes(rdm);
                index = BitConverter.ToUInt64(rdm);


                UInt64[][] indices = new UInt64[THREADS][];
                for (UInt64 i = 0; i < THREADS; i++)
                {
                    indices[i] = new UInt64[BATCH];
                    for (UInt64 j = 0; j < BATCH; j++)
                    {
                        indices[i][j] = index + i * BATCH + j;
                    }
                }

                var results = new ConcurrentBag<Tuple<UInt64, int>>();

                scanTotal += THREADS * BATCH;

                Parallel.ForEach(indices, idr =>
                {
                    foreach (var idx in idr)
                    {
                        BigInteger input = new BigInteger(idx);

                        input = ((c_a * input) + c_c) % c_m;
                        input ^= input >> 1098239;

                        //Optimization: x % 2^n == x & (2^n - 1)
                        input ^= (input & c_maskone) << 698879;

                        //Optimization: x % 2^n == x & (2^n - 1)
                        input ^= (input & c_masktwo) << 1497599;

                        input ^= input >> 1797118;

                        int dotCount = 0;
                        while (true)
                        {
                            int color = (int)(input & (4096 - 1));
                            //int g = (color >> 4) & 0xF;
                            //int r = (color >> 8) & 0xF;
                            //int b = color & 0xF;

                            bool found = color == 0x000;

                            if (!found)
                                break;

                            input = input >> 12;
                            dotCount++;
                        }
                        results.Add(new Tuple<UInt64, int>(idx, dotCount));
                    }
                });

                var oList = results.ToList();
                oList = oList.OrderBy(x => x.Item1).ToList();

                if (oList.Any(x => x.Item2 != 0))
                {
                    for (int i = 0; i < oList.Count; i++)
                    {
                        if(oList[i].Item2 == 0)
                            continue;
                        
                        if (!File.Exists(fName))
                            File.Create(fName);
                        
                        File.AppendAllText(fName, $"Max {oList[i].Item2} BlkDots in Index {oList[i].Item1}\r\n");
                        foundList.Add(oList[i]);
                    }
                }

                if (doPrint)
                {
                    string clrTxt = "";
                    for (int i = 0; i < 32; i++)
                        clrTxt = clrTxt + "\r\n";
                    Console.Write(clrTxt);

                    foreach (var o in foundList)
                        Console.WriteLine($"{o.Item2} BlkDots in Index {o.Item1}");

                    Console.WriteLine();
                    Console.WriteLine($"Average Rate {scanTotal / (DateTime.Now - startTime).TotalSeconds:#.00} Images Per Second");
                    Console.WriteLine($"Scanned {scanTotal} Images in {(DateTime.Now - startTime):hh\\:mm\\:ss}");
                    Console.WriteLine($"Found {foundList.Count} Matching Images");

                    if (foundList.Count > 0)
                    {
                        int maxDots = foundList.Max(x => x.Item2);
                        var maxVal = foundList.Where(x => x.Item2 == maxDots).First();
                        Console.WriteLine($"{maxDots} Consecutive Dots in Index {maxVal.Item1}");
                    }
                }
            }
        }
    }
}
