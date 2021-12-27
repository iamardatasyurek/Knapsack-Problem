using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KnapsackProblem
{
    public class KnapsackCPU
    {
        // items sayısı sabit
        // item eksiltmeyi sil
        //aldıklarını 1 almadıkların 0

        public int population = 100;
        public int gen_size = 60; // 1 2 3 4 5 6 10 60
        public int knapsack_weight = 5000;
        public double crossover_rate = 0.10;
        public double mutation_rate = 0.2;
        public int iteration = 1000;

        Stopwatch sw = new Stopwatch();
        public KnapsackCPU()
        {
            sw.Start();

            //this.population = population;
            //this.gen_size = gen_size;s
            //this.knapsack_weight = knapsack_weight
            //this.crossover_rate = crossover_rate;
            
            List<Items> items_list = create_items(population, gen_size);
            int a = 0;
            foreach (Items item in items_list)
            {
                Console.WriteLine(a + " - - - " + item.weight + " - - - " + item.amount);
                a++;
            }

            List<int[]> chromosomes = create_chromosomes(items_list, population, gen_size);           
            List<double> fitness_values = fitness(items_list, chromosomes, gen_size, knapsack_weight);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("first chromosomes");
            write(chromosomes);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("first fitness values");
            write(fitness_values);

            int counter = 0;
            while (counter < iteration) 
            {               
                List<double> probability = chromosomes_probability(fitness_values);               
                double[] cumulative = cumulative_sum(probability);
                int[] parents = select_parents(cumulative, crossover_rate);
                if (parents.Length > 1)
                {
                    for (int i = 0; i < parents.Length - 1; i++)
                    {
                        for (int j = i + 1; j < parents.Length; j++)
                        {
                            if (i != j)
                            {
                                crossover(chromosomes, items_list, fitness_values, chromosomes[parents[i]], chromosomes[parents[j]], knapsack_weight);
                            }
                        }
                    }
                }
                else
                    Console.WriteLine("Insufficient Number of Individuals to Crossover");
                
                int mutation_probility_count = Convert.ToInt32(chromosomes.Count * 0.1);
                for (int i = 0; i < mutation_probility_count; i++)
                {
                    Random rnd = new Random();
                    if (rnd.NextDouble() > mutation_rate)
                    {
                        mutation(chromosomes, items_list, fitness_values, chromosomes[rnd.Next(0, chromosomes.Count)], knapsack_weight);
                    }
                }

                var temp_list = new List<Tuple<double, int[]>>();
                for (int i = 0; i < fitness_values.Count; ++i)
                {
                    temp_list.Add(Tuple.Create(fitness_values[i], chromosomes[i]));
                }
                var sorted = temp_list.OrderBy(x => x.Item1);
                fitness_values = sorted.Select(x => x.Item1).ToList();
                chromosomes = sorted.Select(x => x.Item2).ToList(); 

                bool pop_chromosome = true;
                while(pop_chromosome)
                {
                    if (fitness_values.Count == population && chromosomes.Count == population)
                        pop_chromosome=false;
                    else
                    {
                        fitness_values.RemoveAt(0);
                        chromosomes.RemoveAt(0);
                        pop_chromosome= true;
                    }
                }
                
                int control = 0;
                foreach (double d in fitness_values)
                {
                    if (d == 1)
                        control++;
                }
                if (control == fitness_values.Count)
                {
                    Console.WriteLine("The result is reached in the " + counter + "th iteration");
                    counter = iteration;
                }
                else 
                {
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine(counter + " chromosomes");
                    write(chromosomes);
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine(counter + " chromosomes weight");
                    write(chromosomes, items_list);
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine(counter + " fitness values");
                    write(fitness_values);
                    counter++;
                }              
            }
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last chromosomes");
            write(chromosomes);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("last fitness values");
            write(fitness_values);

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Süre: "+sw.ElapsedMilliseconds);
        }
        public static List<Items> create_items(int population, int gen_size)
        {
            List<Items> items_list = new List<Items>();
            Random rnd = new Random();
            bool sufficient = true;
            while (sufficient)
            {
                double total_volume = 0;
                for (int i = 0; i < population; i++)
                {
                    items_list.Add(new Items(rnd.Next(1, population), rnd.Next(1,20)));
                    total_volume += items_list[i].amount;
                }
                if (total_volume < population * gen_size)
                {
                    items_list.Clear();
                    sufficient = true;
                }
                else
                    sufficient = false;

            }
            return items_list;
        }   
        public static List<int[]> create_chromosomes(List<Items> items_list, int population, int gen_size)
        {
            List<int[]> chromosomes = new List<int[]>();         
            Random rnd = new Random();
            for (int i = 0; i < population; i++)
            {
                int[] chromosome = new int[gen_size];
                for (int j = 0; j < gen_size; j++)
                {
                    bool control = true;
                    while(control){
                        int index = rnd.Next(0, items_list.Count);
                        if (items_list[index].amount > 0)
                        {
                            chromosome[j] = index;
                            control = false;
                        }
                        else
                            control = true;
                    }           
                }
                chromosomes.Add(chromosome);
            }
            return chromosomes;
        }
        public static List<double> fitness(List<Items> items_list, List<int[]> chromosomes, int gen_size, int knapsack_weight)
        {
            List<double> fitness_values = new List<double>();
            double total;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                total = 0;
                for (int j = 0; j < gen_size; j++)
                {
                    total += items_list[chromosomes[i][j]].weight; 
                }
                fitness_values.Add(1 / (1 + Math.Abs(total - knapsack_weight)));
            }
            return fitness_values;
        }
        public static double fitness(List<Items> items_list, int[] chromosome, int knapsack_weight)
        {
            double fitness_value =0;
            for (int j = 0; j < chromosome.Length; j++)
            {
                fitness_value += items_list[chromosome[j]].weight; 
            }
            fitness_value= 1 / (1 + Math.Abs(fitness_value - knapsack_weight));
            return fitness_value;
        } 
        public static List<double> chromosomes_probability(List<double> fitness_values) 
        {
            List<double> probability= new List<double>();
            int loop = fitness_values.Count;
            double total= fitness_values.Sum();
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
                    if(random < crossover_rate) 
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
        public static void crossover(List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values, int[] parent1, int[] parent2,int knapsack_weight) // 2 tane dizi gelecek sonra caprazlama sonuclarını cromosomes ekleyuecek ve fitnessa ekleyecek
        {
            Random rnd = new Random();
            int[] chield1 = new int[parent1.Length];
            int[] chield2 = new int[parent1.Length];
            int cut = rnd.Next(0, parent1.Length);
            for (int i = 0; i < cut; i++)
            {
                /*
                chield1[i] = parent1[i];
                chield2[i] = parent2[i];*/
                
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
            {/*
                chield1[i] = parent2[i];
                chield2[i] = parent1[i];*/
                
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
            chromosomes.Add(chield1);
            chromosomes.Add(chield2);
            fitness_values.Add(fitness(items_list, chield1, knapsack_weight));
            fitness_values.Add(fitness(items_list, chield2, knapsack_weight));

        }
        public static void mutation(List<int[]> chromosomes, List<Items> items_list, List<double> fitness_values, int[] chromosome, int knapsack_weight) 
        {
            Random rnd = new Random();
            int gen_count = Convert.ToInt32(chromosome.Length * 0.2);
            for (int i = 0; i < gen_count; i++)
            {
                int selection_gen = rnd.Next(0, chromosome.Length);
                int selection_new_gen = rnd.Next(0, items_list.Count);
                chromosome[selection_gen] = selection_new_gen;
                chromosomes.Add(chromosome);
                fitness_values.Add(fitness(items_list, chromosome, knapsack_weight));
            }
           
        }




        public void write(List<double> list) 
        {
            foreach(var v in list) 
            {
                Console.WriteLine(v);
            }
        }
        public void write(List<int[]> list)
        {
            foreach (int[] i in list)
            {
                int length = i.Length;
                for (int j = 0; j < length; j++)
                {
                    Console.Write(i[j]+" ");
                }
                Console.WriteLine();              
            }
        }
        public void write(List<int[]> list, List<Items> items_list)
        {
            foreach (int[] i in list)
            {
                int weight = 0;
                for (int a = 0; a < i.Length; a++)
                {
                    weight += items_list[i[a]].weight;
                }
                Console.Write(weight+"  ");
                
            }
        }
        public void write(int[] array)
        {
            foreach (var v in array)
            {
                Console.Write(v+" ");
            }
        }
                
    }
}
