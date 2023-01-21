using System;
using System.Collections.Generic;

namespace KnapsackProblem
{
    class Program
    {
        public static void Main(string[] args)
        {
            KnapsackSolver cpu = new KnapsackSolverCpu(10, 10, 150, 0.5, 0.3, 0.5, 100);
            KnapsackSolver gpu = new KnapsackSolverGpu(10, 10, 150, 0.5, 0.3, 0.5, 100);

            Console.WriteLine($"CPU Süre: {cpu._sw.ElapsedMilliseconds}");
        }
    }
}
