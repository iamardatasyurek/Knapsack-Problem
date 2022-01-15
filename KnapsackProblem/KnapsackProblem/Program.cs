using System;
using System.Collections.Generic;

namespace KnapsackProblem
{
    class Program
    {
        public static void Main(string[] args)
        {
            Cpu cpu = new Cpu();
            //CUDATest cdts = new CUDATest();
            Gpu gpu = new Gpu();
            Console.WriteLine("CPU Süre: "+cpu.sw.ElapsedMilliseconds);
        }

    }
}
