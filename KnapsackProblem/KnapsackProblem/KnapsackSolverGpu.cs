using ILGPU;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnapsackProblem
{
    class KnapsackSolverGpu : KnapsackSolver
    {
        private List<int> _itemsWeight;
        private List<int> _itemsAmount;
        public KnapsackSolverGpu(int population, int itemCount, int knapsackWeight, double crossoverRate, double mutationRate, double takeRate, int iteration) :
            base(population, itemCount, knapsackWeight, crossoverRate, mutationRate, takeRate, iteration)
        {

            _sw.Start();

            using Context context = Context.Create(builder => builder.Cuda());
            using Accelerator accelerator = context.GetCudaDevice(0).CreateAccelerator(context);
            Interop.WriteLine(accelerator.Device.ToString());

            _itemsList = CreateItemsAccelerated(accelerator);
            Console.WriteLine("Weight and Amount of Items");
            Write(_itemsList);

            _itemsWeight = new List<int>();
            _itemsAmount = new List<int>();      
            for (int i = 0; i < _itemsList.Count; i++)
            {
                _itemsWeight.Add(_itemsList[i].Weight);
                _itemsAmount.Add(_itemsList[i].Amount);
            }

            _chromosomes = CreateChromosomes();

            _fitnessValues = new List<double>();
            for (int i = 0; i < _population; i++)
            {
                _fitnessValues.Add(FitnessAccelerated(accelerator, _chromosomes[i]));
            }

            _chromosomesAmounts = GetAmounts();
            _chromosomesWeights = GetWeights();
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("First chromosomes and fitness values");
            Write(_chromosomes, _fitnessValues);
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
                                Crossover(accelerator, _chromosomes[parents[i]], _chromosomes[parents[j]]);
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
            Write(_chromosomesWeights, _chromosomesAmounts);

            _sw.Stop();
            Console.WriteLine();
            Console.WriteLine($"GPU Time: {_sw.ElapsedMilliseconds}");

        }

        private List<Item> CreateItemsAccelerated(Accelerator accelerator)
        {
            List<Item> itemsList = new List<Item>();
            int[,] items = new int[_itemCount, 2];
            int range = Convert.ToInt32((_knapsackWeight / _itemCount) * 2);
            int[,] randomArray = CreateRandom2DArray(_itemCount, range);

            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView2D<int, Stride2D.DenseX>,
                ArrayView2D<int, Stride2D.DenseX>>(CreateItemsByKernel);

            using var itemBuffer = accelerator.Allocate2DDenseX<int>(new Index2D(_itemCount, 2));
            using var randomBuffer = accelerator.Allocate2DDenseX<int>(new Index2D(_itemCount, 2));
            itemBuffer.CopyFromCPU(items);
            randomBuffer.CopyFromCPU(randomArray);
            kernel(_itemCount, itemBuffer.View, randomBuffer.View);
            itemsList = ConvertToList(itemBuffer.GetAsArray2D());
            return itemsList;
        }
        private static void CreateItemsByKernel(Index1D index, ArrayView2D<int, Stride2D.DenseX> items, 
            ArrayView2D<int, Stride2D.DenseX> randoms)
        {
            for (int i = 0; i < 2; i++)
            {
                items[index, i] = randoms[index, i];
            }
        }
        private double FitnessAccelerated(Accelerator accelerator,int[] chromosome)
        {
            double[] fitness = new double[_population];
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D<int, Stride1D.Dense>,ArrayView1D<double,Stride1D.Dense>,int,int>(FitnessKernel);
            using var chromosomeBuffer = accelerator.Allocate1D(chromosome);
            using var weightBuffer = accelerator.Allocate1D(_itemsWeight.ToArray());
            using var fitnessBuffer = accelerator.Allocate1D<double>(new Index1D(_population));
            chromosomeBuffer.CopyFromCPU(chromosome);
            weightBuffer.CopyFromCPU(_itemsWeight.ToArray());
            fitnessBuffer.CopyFromCPU(fitness);
            kernel(_population, chromosomeBuffer.View, weightBuffer.View, fitnessBuffer.View, _knapsackWeight,chromosome.Length);
            fitness = fitnessBuffer.GetAsArray1D();

            return 1 / (1 + Math.Abs(fitness.Sum() - _knapsackWeight)); 
        }
        private static void FitnessKernel(Index1D index, ArrayView1D<int, Stride1D.Dense> chromosomes, ArrayView1D<int, Stride1D.Dense> itemWeights,
        ArrayView1D<double, Stride1D.Dense> fitness, int knapsackWeight, int chromosomeLength)
        {
            fitness[index] += chromosomes[index] * itemWeights[index];
        }

        private void Crossover(Accelerator accelerator, int[] parent1, int[] parent2)
        {
            int[,] chields = CrossoverAccelerated(accelerator, parent1, parent2);
            int[] chield1 = new int[_itemsList.Count];
            int[] chield2 = new int[_itemsList.Count];
            for (int i = 0; i < _itemsList.Count; i++)
            {
                chield1[i] = chields[0, i];
                chield2[i] = chields[1, i];
            }
            
            int chield1Weight = 0;
            int chield2Weight = 0;
          
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
        private int[,] CrossoverAccelerated(Accelerator accelerator, int[] parent1, int[] parent2)
        {
            var kernel = accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView1D<int, Stride1D.Dense>,
                ArrayView1D<int, Stride1D.Dense>, ArrayView2D<int, Stride2D.DenseX> , RNGView<XorShift64Star>> (CrossoverKernel);
            using var parent1Buffer = accelerator.Allocate1D(parent1);
            using var parent2Buffer = accelerator.Allocate1D(parent2);
            using var childsBuffer = accelerator.Allocate2DDenseX<int>(new Index2D(2,parent1.Length));
            var random = new Random();
            using var rnd = RNG.Create<XorShift64Star>(accelerator, random);
            var rndView = rnd.GetView(accelerator.WarpSize);
            kernel(parent1.Length, parent1Buffer.View, parent2Buffer.View, childsBuffer.View, rndView);
            int[,] childs = childsBuffer.GetAsArray2D();
            return childs;
        }
        
        private static void CrossoverKernel(Index1D index, ArrayView1D<int, Stride1D.Dense> parent1, ArrayView1D<int, Stride1D.Dense> parent2,
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

        private int[,] CreateRandom2DArray(int length, int range)
        {
            int[,] randomArray = new int[length, 2];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (j % 2 == 0)
                        randomArray[i, j] = _rnd.Next(Convert.ToInt32(range / 2), range);
                    else
                        randomArray[i, j] = _rnd.Next(1, 1000);
                }
            }
            return randomArray;
        }
        private List<Item> ConvertToList(int[,] array)
        {
            List<Item> items = new List<Item>();
            for (int i = 0; i < array.Length / 2; i++)
            {
                var item = new Item(array[i, 0], array[i, 1]);
                items.Add(item);
            }
            return items;
        }
    }
}
