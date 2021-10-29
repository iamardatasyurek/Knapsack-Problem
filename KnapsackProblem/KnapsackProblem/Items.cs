using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnapsackProblem
{
    public class Items
    {
        public double weight;
        public int volume;
        public Items(double weight, int volume) 
        {
            this.weight = weight;
            this.volume = volume;  
        }
        public void decrease_Volume() 
        {
            if (this.volume > 0)
                this.volume--;
            else
                this.volume = 0;
        }
    }
}
