using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnapsackProblem
{
    public class KnapsackCPU
    {
        ////1 - x adet item olustur
        ////2 - itemleri canta agırlıgını gemıyecek ve 0 adet olmayacak
        ////    sekılde al max 4 - 6 adet item al(çantalar sabit -random?)
        ////3 - bu sekılde n sayıda kromozon olustur
        ////4 - fitness hesapla
        ////5 - kumulatıf olasılık tablosundan rasgele sayı
        ////    uygulayarak parentları sec
        //6 - çaprazla - fitness uygula - tabloya ekle  -- null olan elemanı cıkartıp tekrar ekle
        //7 - n. %20 bireye mutasyon - fitness uygula - tabloya ekle -- null olan elemanı cıkartıp tekrar ekle
        //8 - sortla 
        //9 - n sayıda birey kalacak sekılde en alttakılerı diziden at
        //10 - bu adımı m sayıda tekrarla

        public int population = 10;
        public int gen_size = 6;
        public int knapsack_weight = 50;

        List<Items> items_list = new List<Items>();

        public KnapsackCPU() 
        {
            //this.population = population;
            //this.gen_size = gen_size;
            //this.knapsack_weight = knapsack_weight

            create_items(items_list, population);
            items_list.Add(new Items(0, 0)); //null item
            int[,] chromosomes = create_chromosomes(items_list, population, gen_size, knapsack_weight);
            List<double> fitness_values = fitness(items_list, chromosomes, population, gen_size, knapsack_weight);
            List<double> probability = chromosomes_probability(fitness_values);
            double[] cumulative = cumulative_sum(probability);
            double[] rassals = get_Rassals(population);
            int[] parents = select_parents(rassals, cumulative);


            /*
            int a = 0;
            foreach (Items item in items_list) 
            {
                Console.WriteLine(a + " - - - " +item.weight + " - - - " + item.volume);
                a++;
            }
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("chromosomes");
            write(chromosomes, population, gen_size);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("fitness values");
            write(fitness_values);
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("Probility");
            write(probability);
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("cumulative");
            write(cumulative.ToList<double>());
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("rassal");
            write(rassals.ToList<double>());
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("parents");
            write(parents);
            */

            



            /*
            int[,] chromosomes = create_chromosomes(items_list,population,gen_size,knapsack_weight);
            */

            /*         
            double[] distanceCount = Distance(items_list,chromosomes, population, gen_size, knapsack_weight);
            double[] distanceHesap = DistanceHesap(distanceCount);
            double[] distanceKumulatif = DistanceKumulatif(distanceHesap);
            double[] kumulatifToplam = ToplamKumulatif(distanceKumulatif);

            for (int i = 0; i < population; i++)
            {
                Console.WriteLine(distanceCount[i] + " - - - " + distanceHesap[i] + " - - - " + distanceKumulatif[i] + " - - - " + kumulatifToplam[i]);
            }
            Console.WriteLine("--------------------");
            */

            /*
            double[] fitness_values = fitness(items_list, chromosomes, population, gen_size, knapsack_weight);     
            double[] rassals = get_Rassals(population);
            int[] parents = select_parents(rassals,fitness_values);
            */
        }
        public static void create_items(List<Items> items_list, int population)
        {
            Random rnd = new Random();
            for (int i = 0; i < population; i++)
            {
                items_list.Add(new Items(rnd.NextDouble() * 10, rnd.Next(1, 20)));
            }
        }
        public static int[,] create_chromosomes(List<Items> items_list, int population, int gen_size, int knapsack_weight) 
        {
            Items[] items_array = items_list.ToArray();
            int[,] chromosomes = new int[population,gen_size]; 
            Random rnd = new Random();
            for (int i = 0; i < population; i++)
            {
                double chromosome_weight = 0;
                for (int j = 0; j < gen_size; j++)
                {
                    int index = rnd.Next(0, items_array.Length-1);
                    if (chromosome_weight + items_array[index].weight < knapsack_weight && items_array[index].volume > 0)
                    {
                        chromosome_weight += items_array[index].weight;
                        items_array[index].decrease_Volume();
                        chromosomes[i, j] = index;
                    }
                    else 
                    {
                        chromosomes[i, j] = items_array.Length-1; //null item
                    }
                }
            }
            return chromosomes;
        }

        public static double fitness(List<Items> items_list, int[] chromosome, int gen_size, int knapsack_weight)
        {
            Items[] items_array = items_list.ToArray();
            double fitness_value =0;
            for (int j = 0; j < gen_size; j++)
            {
                fitness_value += items_array[chromosome[j]].weight;
            }
            fitness_value= 1 / (1 + Math.Abs(fitness_value - knapsack_weight));
            return fitness_value;
        }
        public static List<double> fitness(List<Items> items_list, int[,] chromosomes, int population, int gen_size, int knapsack_weight) 
        {
            Items[] items_array = items_list.ToArray();
            List<double> fitness_values = new List<double>();
            double total;
            for (int i = 0; i < population; i++)
            {
                total = 0;
                for (int j = 0; j < gen_size; j++)
                {
                    total += items_array[chromosomes[i, j]].weight;
                }
                fitness_values.Add(1 / (1 + Math.Abs(total - knapsack_weight)));
            }
            return fitness_values;
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
        public static int[] select_parents(double[] rassals, double[] cumulative) 
        {
            List<int> parents = new List<int>();
            for (int i = 0; i < rassals.Length; i++)
            {
                for (int a = 0; a < rassals.Length; a++)
                {
                    if (cumulative[a] > rassals[i])
                    {
                        parents.Add(a);
                        break;
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
        public void write(List<double> list) 
        {
            foreach(var v in list) 
            {
                Console.WriteLine(v);
            }
        }
        public void write(int[] array)
        {
            foreach (var v in array)
            {
                Console.Write(v+" ");
            }
        }
        public void write(int[,] arrays, int population, int gen_size) 
        {
            for (int i = 0; i < population; i++)
            {
                for (int j = 0; j < gen_size; j++)
                {
                    Console.Write(arrays[i,j]+" ");
                }
                Console.WriteLine();
            }
        }






























        /*
        public static double[] fitness(List<Items> items_list, int[,] chromosomes, int population, int gen_size, int knapsack_weight) 
        {
            Items[] items_array = items_list.ToArray();
            double total = 0;
            double[] fitness_values = new double[population];
            for (int i = 0; i < population; i++)
            {
                total = 0;
                for (int j = 0; j < gen_size; j++)
                {
                    total += items_array[chromosomes[i, j]].weight;  
                }
                total -= knapsack_weight;
                fitness_values[i] = Math.Abs(total);
            }
            double[] temp = new double[fitness_values.Length]; 
            total = 0;
            for (int i = 0; i < fitness_values.Length; i++)
            {
                total = fitness_values[i] + 1;
                temp[i] = 1 / Convert.ToDouble(total);
            }          
            for (int i = 0; i < fitness_values.Length; i++) 
            {
                fitness_values[i] = temp[i];
            }
            total = 0;
            for (int i = 0; i < fitness_values.Length; i++)
            {
                total += fitness_values[i];
            }
            for (int i = 0; i < fitness_values.Length; i++)
            {
                temp[i] = fitness_values[i] / total;
            }
            for (int i = 0; i < fitness_values.Length; i++)
            {
                fitness_values[i] = temp[i];
            }
            total = 0;
            for (int i = 0; i < fitness_values.Length; i++)
            {
                total  += fitness_values[i];
                temp[i] = total;
            }
            for (int i = 0; i < fitness_values.Length; i++)
            {
                fitness_values[i] = temp[i];
            }
            return fitness_values;
        }
        */

        /*
        public static double[] Distance(List<Items> items_List,int[,] items_Index, int population, int gen_Size, int knapsack_Weight)
        {
            Items[] items_Array = items_List.ToArray();
            double toplam = 0;
            double[] toplamCount = new double[population];
            for (int i = 0; i < population; i++)
            {
                toplam = 0;
                for (int j = 0; j < gen_Size; j++)
                {
                    toplam +=  items_Array[items_Index[i,j]].weight;                 
                }
                toplam -= knapsack_Weight;
                toplamCount[i] = Math.Abs(toplam);
            }
            return toplamCount;
        }
        public static double[] DistanceHesap(double[] distanceCount)
        {
            double[] temp = new double[distanceCount.Length];
            double toplam;
            for (int i = 0; i < distanceCount.Length; i++)
            {
                toplam = distanceCount[i] + 1;
                temp[i] = 1 / Convert.ToDouble(toplam);
            }
            return temp;
        }
        public static double[] DistanceKumulatif(double[] distanceHesap)
        {
            double toplam = 0;
            for (int i = 0; i < distanceHesap.Length; i++)
            {
                toplam += distanceHesap[i];
            }
            double[] temp = new double[distanceHesap.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = distanceHesap[i] / toplam;
            }
            return temp;
        }
        public static double[] ToplamKumulatif(double[] distanceKumulatif)
        {
            double[] temp = new double[distanceKumulatif.Length];
            double toplam = 0;
            for (int i = 0; i < temp.Length; i++)
            {
                toplam += distanceKumulatif[i];
                temp[i] = toplam;
            }
            return temp;
        }
        */
    }
}
