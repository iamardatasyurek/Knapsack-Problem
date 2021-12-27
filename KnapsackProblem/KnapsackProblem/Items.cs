using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnapsackProblem
{
    public class Items
    {
        public int weight;
        public int amount;
        public Items(int weight, int amount) 
        {
            this.weight = weight;
            this.amount = amount;  
        }
    }
}
