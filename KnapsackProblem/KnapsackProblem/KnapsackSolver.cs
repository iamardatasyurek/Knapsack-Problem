using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KnapsackProblem
{
    public abstract class KnapsackSolver
    {
        protected int _population;
        protected int _itemCount;
        protected int _knapsackWeight;
        protected double _crossoverRate;
        protected double _mutationRate;
        protected double _takeRate;
        protected int _iteration;

        public Stopwatch _sw { get; private set; }
        protected Random _rnd;

        protected List<Item> _itemsList;
        protected List<int[]> _chromosomes;
        protected List<int> _chromosomesAmounts;
        protected List<int> _chromosomesWeights;
        protected List<double> _fitnessValues;

        protected KnapsackSolver(int population, int itemCount, int knapsackWeight, double crossoverRate, double mutationRate, double takeRate, int iteration)
        {
            _population = population;
            _itemCount = itemCount;
            _knapsackWeight = knapsackWeight;
            _crossoverRate = crossoverRate;
            _mutationRate = mutationRate;
            _takeRate = takeRate;
            _iteration = iteration;

            _rnd = new Random();
            _sw = new Stopwatch();
        }

        public List<int[]> CreateChromosomes()
        {
            List<int[]> chromosomes = new List<int[]>();
            List<double> probabilities = new List<double>();
            for (int i = 0; i < _itemsList.Count; i++)
            {
                double probability = 1.0 / Convert.ToDouble(_itemsList.Count);
                probabilities.Add(probability);
            }
            double[] probabilitiesCum = CumulativeSum(probabilities);
            for (int i = 0; i < _population; i++)
            {
                bool control = true;
                int[] chromosome = new int[_itemsList.Count];
                chromosome = ClearArray(chromosome);
                while (control)
                {
                    for (int j = 0; j < chromosome.Length; j++)
                    {
                        double item = _rnd.NextDouble();
                        for (int k = 0; k < probabilitiesCum.Length; k++)
                        {
                            if (item < probabilitiesCum[k])
                            {
                                item = _rnd.NextDouble();
                                if (item < _takeRate)
                                    chromosome[k]++;
                                break;
                            }
                        }
                    }
                    int totalWeight = 0;
                    for (int j = 0; j < chromosome.Length; j++)
                    {
                        totalWeight += chromosome[j] * _itemsList[j].Weight;
                    }
                    if (totalWeight <= _knapsackWeight)
                        control = false;
                    else
                    {
                        control = true;
                        chromosome = ClearArray(chromosome);
                    }
                }
                chromosomes.Add(chromosome);
            }
            return chromosomes;
        }
        public double Fitness(int[] chromosome)
        {
            double fitnessValue = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                fitnessValue += chromosome[i] * _itemsList[i].Weight;
            }
            fitnessValue = 1 / (1 + Math.Abs(fitnessValue - _knapsackWeight));
            return fitnessValue;
        }
        public List<double> ChromosomesProbability(List<double> fitnessValues)
        {
            List<double> probabilities = new List<double>();
            double total = fitnessValues.Sum();
            for (int i = 0; i < fitnessValues.Count; i++)
            {
                probabilities.Add(fitnessValues[i] / total);
            }
            return probabilities;
        }
        public double[] CumulativeSum(List<double> probabilities)
        {
            double[] probabilitiesArray = probabilities.ToArray();
            double sum = 0;
            for (int i = 0; i < probabilitiesArray.Length; i++)
            {
                sum += probabilitiesArray[i];
                probabilitiesArray[i] = sum;
            }
            return probabilitiesArray;
        }
        public double[] GetRassals(int population)
        {
            double[] rassals = new double[population];
            for (int i = 0; i < rassals.Length; i++)
            {
                rassals[i] = _rnd.NextDouble();
            }
            return rassals;
        }
        public int[] SelectParents(double[] cumulative)
        {
            double[] rassals = GetRassals(cumulative.Length);
            List<int> parents = new List<int>();
            for (int i = 0; i < cumulative.Length; i++)
            {
                for (int j = 0; j < cumulative.Length; j++)
                {
                    double random = _rnd.NextDouble();
                    if (random < _crossoverRate)
                    {
                        if (cumulative[j] > rassals[i])
                        {
                            parents.Add(j);
                            break;
                        }
                    }
                }
            }
            int[] parentsArray = parents.ToArray();
            List<int> parentsTotal = new List<int>();
            for (int i = 0; i < parentsArray.Length; i++)
            {
                if (!parentsTotal.Contains(parentsArray[i]))
                    parentsTotal.Add(parentsArray[i]);
            }
            return parentsTotal.ToArray();
        }
        public void Mutation(int[] chromosome)
        {
            int genCount = _rnd.Next(Convert.ToInt32(chromosome.Length * 0.2), chromosome.Length);
            for (int i = 0; i < genCount; i++)
            {
                int selectionGen = _rnd.Next(0, chromosome.Length);
                int selectionNewGen = _rnd.Next(0, 5);
                chromosome[selectionGen] = selectionNewGen;
            }
            int totalWeight = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                totalWeight += chromosome[i] * _itemsList[i].Weight;
            }
            if (totalWeight <= _knapsackWeight)
            {
                _chromosomes.Add(chromosome);
                _fitnessValues.Add(Fitness(chromosome));
                _chromosomesAmounts.Add(GetNewAmount(chromosome));
                _chromosomesWeights.Add(GetNewWeight(chromosome));
            }
        }
        public List<int> GetAmounts()
        {
            List<int> amounts = new List<int>();
            for (int i = 0; i < _chromosomes.Count; i++)
            {
                int total = GetNewAmount(_chromosomes[i]);
                amounts.Add(total);
            }
            return amounts;
        }
        public int GetNewAmount(int[] chromosome)
        {
            int amount = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                amount += chromosome[i] * _itemsList[i].Amount * _itemsList[i].Weight;
            }
            return amount;
        }
        public List<int> GetWeights()
        {
            List<int> weights = new List<int>();
            for (int i = 0; i < _chromosomes.Count; i++)
            {
                int total = GetNewWeight(_chromosomes[i]);
                weights.Add(total);
            }
            return weights;
        }
        public int GetNewWeight(int[] chromosome)
        {
            int weight = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                weight += chromosome[i] * _itemsList[i].Weight;
            }
            return weight;
        }
        public int[] ClearArray(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 0;
            }
            return array;
        }
        public void OrderAndRemove()
        {
            var tempList = new List<Tuple<int, double, int[], int>>();
            for (int i = 0; i < _chromosomesAmounts.Count; i++)
            {
                tempList.Add(Tuple.Create(_chromosomesAmounts[i], _fitnessValues[i], _chromosomes[i], _chromosomesWeights[i]));
            }
            var sorted = tempList.OrderBy(x => x.Item1);
            _chromosomesAmounts = sorted.Select(x => x.Item1).ToList();
            _fitnessValues = sorted.Select(x => x.Item2).ToList();
            _chromosomes = sorted.Select(x => x.Item3).ToList();
            _chromosomesWeights = sorted.Select(x => x.Item4).ToList();

            bool popChromosome = true;
            while (popChromosome)
            {
                if (_fitnessValues.Count == _population && _chromosomes.Count == _population && _chromosomesAmounts.Count == _population
                    && _chromosomesWeights.Count == _population)
                {
                    popChromosome = false;
                }
                else
                {
                    _chromosomesWeights.RemoveAt(0);
                    _chromosomesAmounts.RemoveAt(0);
                    _fitnessValues.RemoveAt(0);
                    _chromosomes.RemoveAt(0);
                    popChromosome = true;
                }
            }
        }
        public void Write(List<Item> list)
        {
            _sw.Start();
            int i = 0;
            Console.WriteLine("Index - - - Weight - - - Amount");
            foreach (Item item in list)
            {
                Console.WriteLine($"{i} - - - - - -  {item.Weight} - - - - - - {item.Amount}");
                i++;
            }
            _sw.Stop();
        }
        public void Write(List<int> list1, List<int> list2)
        {
            _sw.Start();
            for (int i = 0; i < list1.Count; i++)
            {
                Console.WriteLine($"{list1[i]} kg - - - {list2[i]} $");
            }
            _sw.Stop();
        }
        public void Write(List<int[]> list1, List<double> list2)
        {
            _sw.Start();
            int a = 0;
            foreach (int[] i in list1)
            {
                int length = i.Length;
                for (int j = 0; j < length; j++)
                {
                    Console.Write($"{i[j]} ");
                }
                Console.Write($" - - - {list2[a]}");
                Console.WriteLine();
                a++;
            }
            _sw.Stop();
        }


    }
}
