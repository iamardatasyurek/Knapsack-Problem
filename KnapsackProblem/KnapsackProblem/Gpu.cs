﻿using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
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

            

            List<int[]> items_values = items_valueses(item_count, knapsack_weight);
            List<Items> items_list = create_items(items_values);
            Interop.WriteLine("Items weight and amounts");
            write(items_list);

            List<int[]> chromosomes = create_chromosomes(accelerator,items_list, population, take_rate, knapsack_weight);
            List<double> fitness_values = fitness(items_list, chromosomes, knapsack_weight);
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
                                crossover(chromosomes, items_list, fitness_values, chromosomes_amounts, chromosomes_weights, chromosomes[parents[i]], chromosomes[parents[j]], knapsack_weight);
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
                    if (fitness_values.Count == population && chromosomes.Count == population && chromosomes_amounts.Count == population && chromosomes_weights.Count == population)
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
            sw.Start();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last chromosomes and fitness values");
            write(chromosomes, fitness_values);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last chromosomes weight and amounts");
            write(chromosomes_weights, chromosomes_amounts);

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("GPU Süre: " + sw.ElapsedMilliseconds);
        }
        public static List<int[]> items_valueses(int item_count, int knapsack_weight) 
        {
            sw.Start();
            List<int[]> values = new List<int[]>();
            Random rnd = new Random();
            int range = Convert.ToInt32((knapsack_weight / item_count) * 2);
            
            for (int i = 0; i < item_count; i++)
            {
                int[] item_value = new int[2];
                item_value = zeroArray(item_value);
                for (int j = 0; j < item_value.Length; j++)
                {
                    if (j == 0)
                    {
                        item_value[0] = rnd.Next(Convert.ToInt32(range / 2), range);

                        //For 1-0 Knapsack Problem
                        //item_value[0] = rnd.Next(range, range * 2);
                    }
                    else if (j == 1) 
                        item_value[1] = rnd.Next(1, 1000);
                }
                values.Add(item_value);
            }


            return values;
            sw.Stop();
        }
        public static List<Items> create_items(List<int[]> items_values)
        {
            sw.Start();
            List<Items> items_list = new List<Items>();
            for (int i = 0; i < items_values.Count; i++)
            {
                items_list.Add(new Items(items_values[i][0],items_values[i][1]));
            }
            return items_list;
            sw.Stop();
        }
        public static List<int[]> create_chromosomes(Accelerator accelerator, List<Items> items_list, int population, double take_rate, int knapsack_weight)
        {
            sw.Start();
            List<int[]> chromosomes = new List<int[]>();
            List<double> p = new List<double>();
            for (int i = 0; i < items_list.Count; i++)
            {
                double d = 1.0 / Convert.ToDouble(items_list.Count);
                p.Add(d);
            }
            double[] p_cum = cumulative_sum(p);
            Random rnd = new Random();


            var buffer = accelerator.Allocate1D<int>(1024);
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                         Index1D, ArrayView<int>, int>(create_chromosomes_kernel);
            kernel((int)buffer.Extent, buffer.View, 1);





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







            accelerator.Dispose();

            return chromosomes;
            sw.Stop();
        }
        public static List<double> fitness(List<Items> items_list, List<int[]> chromosomes, int knapsack_weight)
        {
            sw.Start();
            List<double> fitness_values = new List<double>();
            double total;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                total = 0;
                for (int j = 0; j < chromosomes[i].Length; j++)
                {
                    total += chromosomes[i][j] * items_list[j].weight;
                }
                fitness_values.Add(1 / (1 + IntrinsicMath.Abs(total - knapsack_weight)));
            }
            return fitness_values;
            sw.Stop();
        }
        public static double fitness(List<Items> items_list, int[] chromosome, int knapsack_weight)
        {
            sw.Start();
            double fitness_value = 0;
            for (int j = 0; j < chromosome.Length; j++)
            {
                fitness_value += chromosome[j] * items_list[j].weight;
            }
            fitness_value = 1 / (1 + IntrinsicMath.Abs(fitness_value - knapsack_weight));
            return fitness_value;
            sw.Stop();
        }
        public static List<double> chromosomes_probability(List<double> fitness_values)
        {
            sw.Start();
            List<double> probability = new List<double>();
            int loop = fitness_values.Count;
            double total = fitness_values.Sum();
            for (int i = 0; i < loop; i++)
            {
                probability.Add(fitness_values[i] / total);
            }
            return probability;
            sw.Stop();
        }
        public static double[] cumulative_sum(List<double> probability)
        {
            sw.Start();
            double[] probability_array = probability.ToArray();
            double toplam = 0;
            for (int i = 0; i < probability_array.Length; i++)
            {
                toplam += probability_array[i];
                probability_array[i] = toplam;
            }
            return probability_array;
            sw.Stop();
        }
        public static double[] get_Rassals(int population)
        {
            sw.Start();
            Random rnd = new Random();
            double[] rassals = new double[population];
            for (int i = 0; i < rassals.Length; i++)
            {
                rassals[i] = rnd.NextDouble();
            }
            return rassals;
            sw.Stop();
        }
        public static int[] select_parents(double[] cumulative, double crossover_rate)
        {
            sw.Start();
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
            sw.Stop();
        }
        public static void crossover(List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values, List<int> chromosomes_amounts, List<int> chromosomes_weights, int[] parent1, int[] parent2, int knapsack_weight) // 2 tane dizi gelecek sonra caprazlama sonuclarını cromosomes ekleyuecek ve fitnessa ekleyecek
        {
            sw.Start();
            Random rnd = new Random();
            int[] chield1 = new int[parent1.Length];
            int[] chield2 = new int[parent1.Length];
            int chield1_weight = 0;
            int chield2_weight = 0;
            int select_crossover = rnd.Next(0, 2);
            if (select_crossover == 0)
            {
                int cut = rnd.Next(0, parent1.Length);
                // Parent 1 = A1 A2 A3 A4 A5 A6 A7 A8 A9 A10
                // Parent 2 = B1 B2 B3 B4 B5 B6 B7 B8 B9 B10
                // Cut Point                |
                // Chield 1 = A1 B2 A3 B4 A5 A6 B7 A8 B9 A10
                // Chield 2 = B1 A2 B3 A4 B5 B6 A7 B8 A9 B10
                for (int i = 0; i < cut; i++)
                {
                    if (i % 2 == 0)
                    {
                        chield1[i] = parent1[i];
                        chield2[i] = parent2[i];
                    }
                    else
                    {
                        chield2[i] = parent1[i];
                        chield1[i] = parent2[i];
                    }
                }
                for (int i = cut; i < parent1.Length; i++)
                {
                    if (i % 2 == 1)
                    {
                        chield1[i] = parent1[i];
                        chield2[i] = parent2[i];
                    }
                    else
                    {
                        chield2[i] = parent1[i];
                        chield1[i] = parent2[i];
                    }
                }
            }
            else if (select_crossover == 1)
            {
                // Parent 1 = A1 A2 A3 A4 A5 A6 A7 A8 A9 A10
                // Parent 2 = B1 B2 B3 B4 B5 B6 B7 B8 B9 B10
                // Cut Point          |           |
                // Chield 1 = A1 A2 A3 B4 B5 B6 B7 A8 A9 A10
                // Chield 2 = B1 B2 B3 A4 A5 A6 A7 B8 B9 B10
                int cut1 = rnd.Next(0, Convert.ToInt32(parent1.Length * 0.6));
                int cut2 = rnd.Next(cut1, parent1.Length - 1);
                for (int i = 0; i < cut1; i++)
                {
                    chield1[i] = parent1[i];
                    chield2[i] = parent2[i];
                }
                for (int i = cut1; i < cut2; i++)
                {
                    chield1[i] = parent2[i];
                    chield2[i] = parent1[i];
                }
                for (int i = cut2; i < chield1.Length; i++)
                {
                    chield1[i] = parent1[i];
                    chield2[i] = parent2[i];
                }
            }
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
            sw.Stop();
        }
        public static void mutation(List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values, List<int> chromosomes_amounts, List<int> chromosomes_weights, int[] chromosome, int knapsack_weight)
        {
            sw.Start();
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
            sw.Stop();
        }



        public static List<int> getAmounts(List<Items> items_list, List<int[]> chromosomes)
        {
            sw.Start();
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
            sw.Stop();
        }
        public static int getAmounts(List<Items> items_list, int[] chromosome)
        {
            sw.Start();
            int amount = 0; ;
            for (int i = 0; i < chromosome.Length; i++)
            {
                amount += chromosome[i] * items_list[i].amount * items_list[i].weight;
            }
            return amount;
            sw.Stop();
        }
        public static List<int> getWeights(List<Items> items_list, List<int[]> chromosomes)
        {
            sw.Start();
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
            sw.Stop();
        }
        public static int getWeights(List<Items> items_list, int[] chromosome)
        {
            sw.Start();
            int weight = 0; ;
            for (int i = 0; i < chromosome.Length; i++)
            {
                weight += chromosome[i] * items_list[i].weight;
            }
            return weight;
            sw.Stop();
        }
        public static int[] zeroArray(int[] array)
        {
            sw.Start();
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 0;
            }
            return array;
            sw.Stop();
        }


        private static void create_chromosomes_kernel(
        Index1D index,
        ArrayView<int> dataView, 
        int constant) 
        {
            dataView[index] = index + constant;
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
