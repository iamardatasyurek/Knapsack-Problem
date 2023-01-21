namespace KnapsackProblem
{
    public class Item
    {
        public int Weight { get; set; }
        public int Amount { get; set; }
        public Item(int Weight, int Amount) 
        {
            this.Weight = Weight;
            this.Amount = Amount;  
        }
    }
}
