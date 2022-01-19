using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.Cuda.API;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnapsackProblem
{
    class Gpu
    {

        public int population = 10;
        public int item_count = 10;
        public int gen_size;
        public int knapsack_weight = 150;
        public double crossover_rate = 0.05;
        public double mutation_rate = 0.3;
        public double take_rate = 0.5;
        public int iteration = 100;

        public static Stopwatch sw = new Stopwatch();
        Random rnd = new Random();
        public Gpu()
        {
            using Context context = Context.Create(builder => builder.Cuda());
            using Accelerator accelerator = context.GetCudaDevice(0).CreateAccelerator(context);
            Interop.WriteLine(accelerator.Device.ToString());
            
            //this.population = population;
            this.gen_size = item_count;
            //this.knapsack_weight = knapsack_weight
            //this.crossover_rate = crossover_rate;

            List<Items> items_list = create_items_accelerated(accelerator, item_count, knapsack_weight);
            List<int> items_weight = new List<int>();
            List<int> items_amount = new List<int>();
            for (int i = 0; i < items_list.Count; i++)
            {
                items_weight.Add(items_list[i].weight);
                items_amount.Add(items_list[i].amount);
            }
            List<int[]> chromosomes = create_chromosomes_accelerated(accelerator, items_weight, population, take_rate, knapsack_weight);
            foreach(int[] a in chromosomes) 
            {
                for (int i = 0; i < a.Length; i++)
                {
                    Console.Write(a[i]+" ");
                }
                Console.WriteLine(  );
            }

        }

        public static List<Items> create_items_accelerated(Accelerator accelerator, int item_count, int knapsack_weight)
        {
            List<Items> items_list = new List<Items>();
            int[,] items_array = new int[item_count, 2];
            int range = Convert.ToInt32((knapsack_weight / item_count) * 2);
            int[,] random_array = create_random_2DArray(item_count, range);

            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView2D<int, Stride2D.DenseX>,
                ArrayView2D<int, Stride2D.DenseX>>(create_items_kernel);

            using var item_buffer = accelerator.Allocate2DDenseX<int>(new Index2D(item_count, 2));
            using var random_buffer = accelerator.Allocate2DDenseX<int>(new Index2D(item_count, 2));
            item_buffer.CopyFromCPU(items_array);
            random_buffer.CopyFromCPU(random_array);
            kernel(item_count, item_buffer.View, random_buffer.View);
            items_list = convert_to_list(item_buffer.GetAsArray2D());
            return items_list;
        }
        private static void create_items_kernel(Index1D index, ArrayView2D<int, Stride2D.DenseX> items, 
            ArrayView2D<int, Stride2D.DenseX> randoms)
        {
            for (int i = 0; i < 2; i++)
            {
                items[index, i] = randoms[index, i];
            }
        }
        private static int[,] create_random_2DArray(int length, int range)
        {
            int[,] random_array = new int[length, 2];
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (y % 2 == 0)
                        random_array[x, y] = new Random().Next(Convert.ToInt32(range / 2), range);
                    else
                        random_array[x, y] = new Random().Next(1, 1000);
                }
            }
            return random_array;
        }
        private static List<Items> convert_to_list(int[,] array)
        {
            List<Items> items = new List<Items>();
            for (int i = 0; i < array.Length / 2; i++)
            {
                var item = new Items(array[i, 0], array[i, 1]);
                items.Add(item);
            }
            return items;
        }
        public static void create_chromosome_kernel(Index1D index, ArrayView1D<int, Stride1D.Dense> items_weight_list, 
            ArrayView1D<int, Stride1D.Dense> chromosome, double take_rate,
            int knapsack_weight, RNGView<XorShift64Star> rng,ArrayView1D<double, Stride1D.Dense> p_cum)
        {
            bool control = true;
            while (control)
            {
                for (int i = 0; i < chromosome.Length; i++)
                {
                    if (rng.NextDouble() < p_cum[index])
                    {
                        if (rng.NextDouble() < take_rate)
                            chromosome[index]++;

                    }
                    //For 1-0 Knapsack Problem
                    /*
                    if (rng.NextDouble() < take_rate)
                        chromosome[index]=1;
                    */
                }
                int total_weight = 0;
                for (int i = 0; i < chromosome.Length; i++)
                {
                    total_weight += chromosome[i] * items_weight_list[i];
                }
                if (total_weight <= knapsack_weight)
                    control = false;
                else
                {
                    control = true;
                    for (int i = 0; i < chromosome.Length; i++)
                    {
                        chromosome[i] = 0;
                    }
                }
            }
        }
        public static List<int[]> create_chromosomes_accelerated(Accelerator accelerator, List<int> items_weight_list, 
            int population, double take_rate, int knapsack_weight)
        {
            List<int[]> chromosomes = new List<int[]>();
            List<double> p = new List<double>();
            for (int i = 0; i < items_weight_list.Count; i++)
            {
                double d = 1.0 / Convert.ToDouble(items_weight_list.Count);
                p.Add(d);
            }
            double[] p_cum = cumulative_sum(p);
            Random rnd = new Random();
            int[] chromosome = new int[items_weight_list.Count];

            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D< int, Stride1D.Dense >, 
                double,
                int, 
                RNGView< XorShift64Star >,
                ArrayView1D<double, Stride1D.Dense>> (create_chromosome_kernel);

            var random = new Random();
            using var chromosome_buffer = accelerator.Allocate1D(chromosome);
            using var item_weight_buffer = accelerator.Allocate1D(items_weight_list.ToArray());
            using var random_buffer = accelerator.CreateRNG<XorShift64Star>(random); 
            using var p_cum_buffer = accelerator.Allocate1D(p_cum);
            chromosome_buffer.CopyFromCPU(chromosome);
            item_weight_buffer.CopyFromCPU(items_weight_list.ToArray());
            p_cum_buffer.CopyFromCPU(p_cum);
            var random_buffer_view = random_buffer.GetView(accelerator.WarpSize);

            for (int j = 0; j < population; j++)
            {
                chromosome = zeroArray(chromosome);
                kernel(population, item_weight_buffer.View, chromosome_buffer.View,take_rate,knapsack_weight, random_buffer_view, p_cum_buffer.View);
                chromosomes.Add(chromosome);
            }
            return chromosomes;
        }


















        public static double[] cumulative_sum(List<double> probability)
        {
            double[] probability_array = probability.ToArray();
            double toplam = 0;
            for (int i = 0; i < probability_array.Length; i++)
            {
                toplam += probability_array[i];
                probability_array[i] = toplam;
            }
            return probability_array;
        }
        public static int[] zeroArray(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 0;
            }
            return array;
        }



        public void write(List<Items> list)
        {
            sw.Start();
            int a = 0;
            Interop.WriteLine("index - - - Weight - - - Amount");
            foreach (Items item in list)
            {
                Interop.WriteLine(a + " - - - - - - " + item.weight + " - - - - - - " + item.amount);
                a++;
            }
            sw.Stop();
        }
        public void write(List<int> list1, List<int> list2)
        {
            sw.Start();
            for (int i = 0; i < list1.Count; i++)
            {
                Interop.WriteLine(list1[i] + " kg - - - " + list2[i] + " $");
            }
            sw.Stop();
        }
        public void write(List<int[]> list1, List<double> list2)
        {
            sw.Start();
            int a = 0;
            foreach (int[] i in list1)
            {
                int length = i.Length;
                for (int j = 0; j < length; j++)
                {
                    Interop.Write(i[j] + " ");
                }
                Interop.Write(" - - -  " + list2[a]);
                Interop.WriteLine("");
                a++;
            }
            sw.Stop();
        }
    }
}
