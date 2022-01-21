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
        public double crossover_rate = 0.5;
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

            sw.Start();

            List<Items> items_list = create_items_accelerated(accelerator, item_count, knapsack_weight);
            Console.WriteLine("Items weight and amounts");
            write(items_list);
            List<int> items_weight = new List<int>();
            List<int> items_amount = new List<int>();      
            for (int i = 0; i < items_list.Count; i++)
            {
                items_weight.Add(items_list[i].weight);
                items_amount.Add(items_list[i].amount);
            }
            List<int[]> chromosomes = create_chromosomes(items_list, population, take_rate, knapsack_weight);
            List<double> fitness_values = new List<double>();
            for (int i = 0; i < population; i++)
            {
                fitness_values.Add(fitness_accelerated(accelerator, items_weight.ToArray(), chromosomes[i],population,knapsack_weight));
            }
            List<int> chromosomes_amounts = getAmounts(items_list, chromosomes);
            List<int> chromosomes_weights = getWeights(items_list, chromosomes);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("first chromosomes and fitness values");
            write(chromosomes, fitness_values);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("first chromosomes weight and amounts");
            write(chromosomes_weights, chromosomes_amounts);


            int counter = 0;
            while (counter < iteration)
            {
                List<double> probability = chromosomes_probability(fitness_values);
                double[] cumulative = cumulative_sum(probability);
                int[] parents = select_parents(cumulative, crossover_rate);
                Console.WriteLine("Parents Count :" + parents.Length);
                if (parents.Length > 1)
                {
                    for (int i = 0; i < parents.Length - 1; i++)
                    {
                        for (int j = i + 1; j < parents.Length; j++)
                        {
                            if (i != j)
                            {
                                crossover(accelerator, chromosomes, items_list, fitness_values, chromosomes_amounts, chromosomes_weights, chromosomes[parents[i]], chromosomes[parents[j]], knapsack_weight);
                            }
                        }
                    }
                }
                else
                    Console.WriteLine("Insufficient Number of Individuals to Crossover");


                int mutation_probility_count = rnd.Next(Convert.ToInt32(chromosomes.Count * 0.3), Convert.ToInt32(chromosomes.Count * 0.5));
                for (int i = 0; i < mutation_probility_count; i++)
                {
                    double random = rnd.NextDouble();
                    if (random < mutation_rate)
                    {
                        mutation(chromosomes, items_list, fitness_values, chromosomes_amounts, chromosomes_weights, chromosomes[rnd.Next(0, chromosomes.Count)], knapsack_weight);
                    }
                }

                var temp_list = new List<Tuple<int, double, int[], int>>();
                for (int i = 0; i < chromosomes_amounts.Count; i++)
                {
                    temp_list.Add(Tuple.Create(chromosomes_amounts[i], fitness_values[i], chromosomes[i], chromosomes_weights[i]));
                }
                var sorted = temp_list.OrderBy(x => x.Item1);
                chromosomes_amounts = sorted.Select(x => x.Item1).ToList();
                fitness_values = sorted.Select(x => x.Item2).ToList();
                chromosomes = sorted.Select(x => x.Item3).ToList();
                chromosomes_weights = sorted.Select(x => x.Item4).ToList();

                bool pop_chromosome = true;
                while (pop_chromosome)
                {
                    if (fitness_values.Count == population && chromosomes.Count == population && chromosomes_amounts.Count == population
                        && chromosomes_weights.Count == population)
                        pop_chromosome = false;
                    else
                    {
                        chromosomes_weights.RemoveAt(0);
                        chromosomes_amounts.RemoveAt(0);
                        fitness_values.RemoveAt(0);
                        chromosomes.RemoveAt(0);
                        pop_chromosome = true;
                    }
                }

                if (counter + 1 != iteration)
                {
                    Console.WriteLine(counter + " chromosomes and fitness values");
                    write(chromosomes, fitness_values);
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine(counter + " chromosomes weight and amounts");
                    write(chromosomes_weights, chromosomes_amounts);
                    Console.WriteLine("--------------------------------------");
                }
                counter++;
            }

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last chromosomes and fitness values");
            write(chromosomes, fitness_values);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last chromosomes weight and amounts");
            write(chromosomes_weights, chromosomes_amounts);

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Süre: " + sw.ElapsedMilliseconds);

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
        public static List<int[]> create_chromosomes(List<Items> items_list, int population, double take_rate, int knapsack_weight)
        {
            List<int[]> chromosomes = new List<int[]>();
            List<double> p = new List<double>();
            for (int i = 0; i < items_list.Count; i++)
            {
                double d = 1.0 / Convert.ToDouble(items_list.Count);
                p.Add(d);
            }
            double[] p_cum = cumulative_sum(p);
            Random rnd = new Random();
            for (int j = 0; j < population; j++)
            {
                bool control = true;
                int[] chromosome = new int[items_list.Count];
                chromosome = zeroArray(chromosome);
                while (control)
                {
                    for (int i = 0; i < chromosome.Length; i++)
                    {
                        double random = rnd.NextDouble();
                        for (int a = 0; a < p_cum.Length; a++)
                        {
                            if (random < p_cum[a])
                            {
                                random = rnd.NextDouble();
                                if (random < take_rate)
                                    chromosome[a]++;
                                break;
                            }
                        }
                        //For 1-0 Knapsack Problem
                        /*
                        if (random < take_rate)
                            chromosome[i]=1;
                        */
                    }
                    int total_weight = 0;
                    for (int i = 0; i < chromosome.Length; i++)
                    {
                        total_weight += chromosome[i] * items_list[i].weight;
                    }
                    if (total_weight <= knapsack_weight)
                        control = false;
                    else
                    {
                        control = true;
                        chromosome = zeroArray(chromosome);
                    }
                }
                chromosomes.Add(chromosome);
            }
            return chromosomes;
        }
        public static double fitness_accelerated(Accelerator accelerator, int[] itemWeights,int[] chromosome, int population,int knapsack_weight)
        {
            double[] fitness = new double[population];
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D<int, Stride1D.Dense>,ArrayView1D<double,Stride1D.Dense>,int,int>(fitness_kernel);
            using var chromosome_buffer = accelerator.Allocate1D(chromosome);
            using var wweight_bugger = accelerator.Allocate1D(itemWeights);
            using var fitness_buffer = accelerator.Allocate1D<double>(new Index1D(population));
            chromosome_buffer.CopyFromCPU(chromosome);
            wweight_bugger.CopyFromCPU(itemWeights);
            fitness_buffer.CopyFromCPU(fitness);
            kernel(population, chromosome_buffer.View, wweight_bugger.View, fitness_buffer.View, knapsack_weight,chromosome.Length);
            fitness = fitness_buffer.GetAsArray1D();

            return 1 / (1 + Math.Abs(fitness.Sum() - knapsack_weight)); 
        }
        public static double fitness(List<Items> items_list, int[] chromosome, int knapsack_weight)
        {
            double fitness_value = 0;
            for (int j = 0; j < chromosome.Length; j++)
            {
                fitness_value += chromosome[j] * items_list[j].weight;
            }
            fitness_value = 1 / (1 + Math.Abs(fitness_value - knapsack_weight));
            return fitness_value;
        }
        private static void fitness_kernel(Index1D index, ArrayView1D<int, Stride1D.Dense> chromosomes, ArrayView1D<int, Stride1D.Dense> item_weights,
        ArrayView1D<double, Stride1D.Dense> fitness, int knapsack_weight, int chromosomeLength)
        {
            fitness[index] += chromosomes[index] * item_weights[index];
        }

        public static void crossover(Accelerator accelerator, List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values,
           List<int> chromosomes_amounts, List<int> chromosomes_weights, int[] parent1, int[] parent2, int knapsack_weight)
        {
            int[,] chields = crossover_accelerated(accelerator, parent1, parent2);
            int[] chield1 = new int[items_list.Count];
            int[] chield2 = new int[items_list.Count];
            for (int i = 0; i < items_list.Count; i++)
            {
                chield1[i] = chields[0, i];
                chield2[i] = chields[1, i];
            }
            
            int chield1_weight = 0;
            int chield2_weight = 0;
          
            for (int i = 0; i < chield1.Length; i++)
            {
                chield1_weight += chield1[i] * items_list[i].weight;
                chield2_weight += chield2[i] * items_list[i].weight;
            }
            if (chield1_weight <= knapsack_weight)
            {
                chromosomes.Add(chield1);
                fitness_values.Add(fitness(items_list, chield1, knapsack_weight));
                chromosomes_amounts.Add(getAmounts(items_list, chield1));
                chromosomes_weights.Add(getWeights(items_list, chield1));

            }
            if (chield2_weight <= knapsack_weight)
            {
                chromosomes.Add(chield2);
                fitness_values.Add(fitness(items_list, chield2, knapsack_weight));
                chromosomes_amounts.Add(getAmounts(items_list, chield2));
                chromosomes_weights.Add(getWeights(items_list, chield2));
            }
        }
        public static int[,] crossover_accelerated(Accelerator accelerator, int[] parent1, int[] parent2)
        {
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D<int, Stride1D.Dense>, ArrayView2D<int, Stride2D.DenseX> , RNGView<XorShift64Star>> (crossover_kernel);
            using var parent1_buffer = accelerator.Allocate1D(parent1);
            using var parent2_buffer = accelerator.Allocate1D(parent2);
            using var childs_buffer = accelerator.Allocate2DDenseX<int>(new Index2D(2,parent1.Length));
            var random = new Random();
            using var rnd = RNG.Create<XorShift64Star>(accelerator, random);
            var rndView = rnd.GetView(accelerator.WarpSize);
            kernel(parent1.Length, parent1_buffer.View, parent2_buffer.View, childs_buffer.View, rndView);
            int[,] childs = childs_buffer.GetAsArray2D();
            return childs;
        }
        
        private static void crossover_kernel(Index1D index, ArrayView1D<int, Stride1D.Dense> parent1, ArrayView1D<int, Stride1D.Dense> parent2,
        ArrayView2D<int, Stride2D.DenseX> childs, RNGView<XorShift64Star> rnd)
        {
                int cut = rnd.Next();

                if(index < cut)
                {
                    if (index % 2 == 0)
                    {
                        childs[0,index] = parent1[index];
                        childs[1,index] = parent2[index];
                    }
                    else
                    {
                        childs[1, index] = parent1[index];
                        childs[0, index] = parent2[index];
                    }
                }
                else
                {
                    if (index % 2 == 1)
                    {
                        childs[1, index] = parent1[index];
                        childs[0, index] = parent2[index];
                    }
                    else
                    {
                        childs[1, index] = parent1[index];
                        childs[0, index] = parent2[index];
                    }
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
        public static List<double> chromosomes_probability(List<double> fitness_values)
        {
            List<double> probability = new List<double>();
            int loop = fitness_values.Count;
            double total = fitness_values.Sum();
            for (int i = 0; i < loop; i++)
            {
                probability.Add(fitness_values[i] / total);
            }
            return probability;
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
        public static double[] get_Rassals(int population)
        {
            Random rnd = new Random();
            double[] rassals = new double[population];
            for (int i = 0; i < rassals.Length; i++)
            {
                rassals[i] = rnd.NextDouble();
            }
            return rassals;
        }
        public static int[] select_parents(double[] cumulative, double crossover_rate)
        {
            double[] rassals = get_Rassals(cumulative.Length);
            List<int> parents = new List<int>();
            Random rnd = new Random();
            for (int i = 0; i < cumulative.Length; i++)
            {
                for (int a = 0; a < cumulative.Length; a++)
                {
                    double random = rnd.NextDouble();
                    if (random < crossover_rate)
                    {
                        if (cumulative[a] > rassals[i])
                        {
                            parents.Add(a);
                            break;
                        }
                    }
                }
            }
            int[] parents_array = parents.ToArray();
            List<int> parents_total = new List<int>();
            for (int i = 0; i < parents_array.Length; i++)
            {
                if (!parents_total.Contains(parents_array[i]))
                    parents_total.Add(parents_array[i]);
            }
            return parents_total.ToArray();
        }
        public static void mutation(List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values,
           List<int> chromosomes_amounts, List<int> chromosomes_weights, int[] chromosome, int knapsack_weight)
        {
            Random rnd = new Random();
            int gen_count = rnd.Next(Convert.ToInt32(chromosome.Length * 0.2), chromosome.Length);
            for (int i = 0; i < gen_count; i++)
            {
                int selection_gen = rnd.Next(0, chromosome.Length);
                int selection_new_gen = rnd.Next(0, 5);
                chromosome[selection_gen] = selection_new_gen;

                //For 1-0 Knapsack Problem
                /*
                if (chromosome[selection_gen] == 1)
                    chromosome[selection_gen] = 0;
                else
                    chromosome[selection_gen] = 1;
                */
            }
            int total_weight = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                total_weight += chromosome[i] * items_list[i].weight;
            }
            if (total_weight <= knapsack_weight)
            {
                chromosomes.Add(chromosome);
                fitness_values.Add(fitness(items_list, chromosome, knapsack_weight));
                chromosomes_amounts.Add(getAmounts(items_list, chromosome));
                chromosomes_weights.Add(getWeights(items_list, chromosome));
            }
        }
        public static List<int> getAmounts(List<Items> items_list, List<int[]> chromosomes)
        {
            List<int> amounts = new List<int>();
            int total;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                total = 0;
                for (int j = 0; j < chromosomes[i].Length; j++)
                {
                    total += chromosomes[i][j] * items_list[j].amount * items_list[j].weight;
                }
                amounts.Add(total);
            }
            return amounts;
        }
        public static int getAmounts(List<Items> items_list, int[] chromosome)
        {
            int amount = 0; ;
            for (int i = 0; i < chromosome.Length; i++)
            {
                amount += chromosome[i] * items_list[i].amount * items_list[i].weight;
            }
            return amount;
        }
        public static List<int> getWeights(List<Items> items_list, List<int[]> chromosomes)
        {
            List<int> weights = new List<int>();
            int total;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                total = 0;
                for (int j = 0; j < chromosomes[i].Length; j++)
                {
                    total += chromosomes[i][j] * items_list[j].weight;
                }
                weights.Add(total);
            }
            return weights;
        }
        public static int getWeights(List<Items> items_list, int[] chromosome)
        {
            int weight = 0; ;
            for (int i = 0; i < chromosome.Length; i++)
            {
                weight += chromosome[i] * items_list[i].weight;
            }
            return weight;
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
