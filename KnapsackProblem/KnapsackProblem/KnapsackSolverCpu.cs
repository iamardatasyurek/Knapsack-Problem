using System;
using System.Collections.Generic;

namespace KnapsackProblem
{
    class KnapsackSolverCpu : KnapsackSolver
    {
        public KnapsackSolverCpu(int population, int itemCount, int knapsackWeight, double crossoverRate, double mutationRate, double takeRate, int iteration) : 
            base(population, itemCount, knapsackWeight, crossoverRate, mutationRate, takeRate, iteration)
        {
            _sw.Start();

            _itemsList = CreateItems();
            Console.WriteLine("Weight and Amount of Items");
            Write(_itemsList);

            _chromosomes = CreateChromosomes();
            _fitnessValues = Fitness();
            _chromosomesAmounts = GetAmounts();
            _chromosomesWeights = GetWeights();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("First chromosomes and fitness values");
            Write(_chromosomes,_fitnessValues);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("First chromosomes weight and amounts");
            Write(_chromosomesWeights, _chromosomesAmounts);
            
            int counter = 0;
            while (counter < _iteration)
            {
                List<double> probabilities = ChromosomesProbability(_fitnessValues);
                double[] cumulative = CumulativeSum(probabilities);
                int[] parents = SelectParents(cumulative);
                Console.WriteLine($"Parents Count : {parents.Length}");
                if (parents.Length > 1)
                {
                    for (int i = 0; i < parents.Length - 1; i++)
                    {
                        for (int j = i + 1; j < parents.Length; j++)
                        {
                            if (i != j)
                            {
                                Crossover(_chromosomes[parents[i]], _chromosomes[parents[j]]);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Insufficient Number of Individuals to Crossover");
                }

                int mutationProbilityCount = _rnd.Next(Convert.ToInt32(_chromosomes.Count * 0.3), Convert.ToInt32(_chromosomes.Count * 0.5));
                for (int i = 0; i < mutationProbilityCount; i++)
                {
                    double random = _rnd.NextDouble();
                    if (random < _mutationRate)
                    {
                        Mutation(_chromosomes[_rnd.Next(0, _chromosomes.Count)]);
                    }
                }

                OrderAndRemove();

                if (counter + 1 != _iteration)
                {
                    Console.WriteLine($"{counter} chromosomes and fitness values");
                    Write(_chromosomes, _fitnessValues);
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine($"{counter} chromosomes weight and amounts");
                    Write(_chromosomesWeights, _chromosomesAmounts);
                    Console.WriteLine("--------------------------------------");
                }
                counter++;
            }

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Last chromosomes and fitness values");
            Write(_chromosomes, _fitnessValues);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Last chromosomes weight and amounts");
            Write(_chromosomesWeights,_chromosomesAmounts);
            
            _sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"CPU Time: {_sw.ElapsedMilliseconds}");
        }
        private List<Item> CreateItems()
        {
            List<Item> itemsList = new List<Item>();
            int range = Convert.ToInt32((_knapsackWeight / _itemCount) *2);
            for (int i = 0; i < _itemCount; i++)
            {
                itemsList.Add(new Item(_rnd.Next(Convert.ToInt32(range/2), range), _rnd.Next(1, 1000)));
            }
            return itemsList;
        }
        private List<double> Fitness()
        {
            List<double> fitnessValues = new List<double>();
            for (int i = 0; i < _chromosomes.Count; i++)
            {
                double total = Fitness(_chromosomes[i]);
                for (int j = 0; j < _chromosomes[i].Length; j++)
                {
                    total += _chromosomes[i][j]*_itemsList[j].Weight;
                }
                fitnessValues.Add(1 / (1 + Math.Abs(total - _knapsackWeight)));
            }
            return fitnessValues;
        }
        private void Crossover(int[] parent1, int[] parent2)
        {
            int[] chield1 = new int[parent1.Length];
            int[] chield2 = new int[parent1.Length];
            int chield1Weight = 0;
            int chield2Weight = 0;
            int selectCrossover = _rnd.Next(0, 2);
            if (selectCrossover == 0)
            {
                int reversePoint = _rnd.Next(0, parent1.Length);

                //      Parent 1 = A1 A2 A3 A4 A5 A6 A7 A8 A9 A10
                //      Parent 2 = B1 B2 B3 B4 B5 B6 B7 B8 B9 B10
                // Reverse Point                 |
                //      Chield 1 = A1 B2 A3 B4 A5 A6 B7 A8 B9 A10
                //      Chield 2 = B1 A2 B3 A4 B5 B6 A7 B8 A9 B10

                for (int i = 0; i < reversePoint; i++)
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
                for (int i = reversePoint; i < parent1.Length; i++)
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
            else if (selectCrossover == 1)
            {
                //      Parent 1 = A1 A2 A3 A4 A5 A6 A7 A8 A9 A10
                //      Parent 2 = B1 B2 B3 B4 B5 B6 B7 B8 B9 B10
                // Change Points           |           |
                //      Chield 1 = A1 A2 A3 B4 B5 B6 B7 A8 A9 A10
                //      Chield 2 = B1 B2 B3 A4 A5 A6 A7 B8 B9 B10

                int cut1 = _rnd.Next(0, Convert.ToInt32(parent1.Length * 0.6));
                int cut2 = _rnd.Next(cut1, parent1.Length - 1);

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
                chield1Weight += chield1[i] * _itemsList[i].Weight;
                chield2Weight += chield2[i] * _itemsList[i].Weight;
            }
            if (chield1Weight <= _knapsackWeight)
            {
                _chromosomes.Add(chield1);
                _fitnessValues.Add(Fitness(chield1));
                _chromosomesAmounts.Add(GetNewAmount(chield1));
                _chromosomesWeights.Add(GetNewWeight(chield1));

            }
            if (chield2Weight <= _knapsackWeight)
            {
                _chromosomes.Add(chield2);
                _fitnessValues.Add(Fitness(chield2));
                _chromosomesAmounts.Add(GetNewAmount(chield2));
                _chromosomesWeights.Add(GetNewWeight(chield2));
            }
        }
    }
}
