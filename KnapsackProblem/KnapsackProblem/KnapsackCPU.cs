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
        public int gen_Size = 6;
        public int knapsack_Weight = 50;

        List<Items> items_List = new List<Items>();

        public KnapsackCPU() 
        {
            //this.population = population;
            //this.gen_Size = gen_Size;
            //this.knapsack_Weight = knapsack_Weight

            create_Items(items_List, population);
            items_List.Add(new Items(0, 0)); //null item

            
            int a = 0;
            foreach (Items item in items_List)
            {
                Console.WriteLine(a + " - - - " + item.weight + " " + item.volume);
                a++;
            }
            Console.WriteLine("--------------------------------");
            

            int[,] items_Index = create_chromosomes(items_List,population,gen_Size,knapsack_Weight);
            
                                                                                                            
            for (int i = 0; i < population; i++)
            {
                for (int j = 0; j < gen_Size; j++)
                {
                    Console.Write(items_Index[i,j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("-----------------------------------");
            
            /*
            double[] distanceCount = Distance(items_List,items_Index, population, gen_Size, knapsack_Weight);
            double[] distanceHesap = DistanceHesap(distanceCount);
            double[] distanceKumulatif = DistanceKumulatif(distanceHesap);
            double[] kumulatifToplam = ToplamKumulatif(distanceKumulatif);

            for (int i = 0; i < population; i++)
            {
                Console.WriteLine(distanceCount[i] + " - - - " + distanceHesap[i] + " - - - " + distanceKumulatif[i] + " - - - " + kumulatifToplam[i]);
            }
            Console.WriteLine("--------------------");
            */

            double[] fitness_Values = fitness(items_List, items_Index, population, gen_Size, knapsack_Weight);     
            double[] rassals = get_Rassals(population);
            Console.WriteLine("Fitness --------------- Rassals");
            Console.WriteLine();
            for (int i = 0; i < rassals.Length; i++)
            {
                Console.WriteLine(fitness_Values[i] + " - - - " + rassals[i]);
            }
            Console.WriteLine("---------------------------------");
            int[] parents = select_Parents(rassals,fitness_Values);
            Console.WriteLine("parents");
            Console.WriteLine();
            foreach (int d in parents)
            {
                Console.Write(d +" ");
            }
            Console.WriteLine();
            Console.WriteLine("---------------------------------");
        }
        public static void create_Items(List<Items> items_List, int population)
        {
            Random rnd = new Random();
            for (int i = 0; i < population; i++)
            {
                items_List.Add(new Items(rnd.NextDouble() * 10, rnd.Next(1, 20)));
            }
        }
        public static int[,] create_chromosomes(List<Items> items_List, int population, int gen_Size, int knapsack_Weight) 
        {
            Items[] items_Array = items_List.ToArray();
            int[,] items_Index = new int[population,gen_Size];
            Random rnd = new Random();
            int count = 0;
            for (int i = 0; i < population; i++)
            {
                double chromosome_Weight = 0;
                for (int j = 0; j < gen_Size; j++)
                {
                    //gen sayısını ben belirlemelimiyim ? 6 olarak belirledim
                    //çaprazlama yaparken sıkıntı olur mu ?
                    int index = rnd.Next(0, items_Array.Length-1); // (0,11-1) -> (0,10) -> 0 - 1 - 2 - 3 - 4 - 5 - 6 - 7 - 8 - 9
                    if (chromosome_Weight + items_Array[index].weight < knapsack_Weight && items_Array[index].volume > 0)
                    {
                        chromosome_Weight += items_Array[index].weight;
                        items_Array[index].decrease_Volume();
                        items_Index[i, j] = index;
                    }
                    else 
                    {
                        count++;
                        items_Index[i, j] = items_Array.Length-1; //null item
                    }
                }
            }
            Console.WriteLine("Null Item Count: "+count);
            Console.WriteLine();
            return items_Index;
        }
        
        public static double[] fitness(List<Items> items_List, int[,] items_Index,int population, int gen_Size, int knapsack_Weight) 
        {
            Items[] items_Array = items_List.ToArray();
            double total = 0;
            double[] fitness_Values = new double[population];
            for (int i = 0; i < population; i++)
            {
                total = 0;
                for (int j = 0; j < gen_Size; j++)
                {
                    total += items_Array[items_Index[i, j]].weight;  
                }
                total -= knapsack_Weight;
                fitness_Values[i] = Math.Abs(total);
            }
            double[] temp = new double[fitness_Values.Length];
            total = 0;
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                total = fitness_Values[i] + 1;
                temp[i] = 1 / Convert.ToDouble(total);
            }
            
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                fitness_Values[i] = temp[i];
            }
            total = 0;
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                total += fitness_Values[i];
            }
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                temp[i] = fitness_Values[i] / total;
            }
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                fitness_Values[i] = temp[i];
            }
            total = 0;
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                total  += fitness_Values[i];
                temp[i] = total;
            }
            for (int i = 0; i < fitness_Values.Length; i++)
            {
                fitness_Values[i] = temp[i];
            }
            return fitness_Values;
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
        public static int[] select_Parents(double[] rassals, double[] fitness_Values) 
        {
            List<int> parents = new List<int>();
            for (int i = 0; i < rassals.Length; i++)
            {
                for (int a = 0; a < rassals.Length; a++)
                {
                    if (fitness_Values[a] > rassals[i])
                    {
                        parents.Add(a);
                        break;
                    }
                }
            }
            int[] parents_Array = parents.ToArray();
            List<int> parents_Total = new List<int>();
            for (int i = 0; i < parents_Array.Length; i++)
            {
                if (!parents_Total.Contains(parents_Array[i]))
                    parents_Total.Add(parents_Array[i]);
            }
            return parents_Total.ToArray();
        }






























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
