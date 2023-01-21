# Knapsack-Problem

In this project, the knapscak problem is solved by genetic algorithm. The solving is compared on CPU and GPU.

Used the [ILGPU](https://github.com/m4rs-mt/ILGPU) library to run through the GPU. 

**Steps** <br/>

Step 1
- Creating items with weight and amount values.
- Creating chromosomes from items.
- Calculating fitness values of chromosomes.

Step 2
- Calculating probabilities from fitness values.
- Calculating cumulative totals of probabilities.

Step 3
- Selection of parents with using cumulative totals
- Crossing genes of the parents

Parents
![parents](https://github.com/iamardatasyurek/Knapsack-Problem/blob/main/images/crossoverparents.png)

Crossover Type 1
![crossovertype1](https://github.com/iamardatasyurek/Knapsack-Problem/blob/main/images/crossovertype1.png)

Crossover Type 2
![crossovertype2](https://github.com/iamardatasyurek/Knapsack-Problem/blob/main/images/crossovertype2.png)

Note: In the GPU calculation, only type 1 crossover was used.

Step 4
- Mutation of 30-50% of the total chromosomes
- Alteraion a random number of genes

![mutation](https://github.com/iamardatasyurek/Knapsack-Problem/blob/main/images/mutation.png)

Step 5
- Ranking by amount value
- Removal of excess chromosomes

Step 6
- Go to Step 2 and repeat as many times as the number iteration

