namespace RoslynScribe.TestProject
{
    internal class S010_InfiniteRecursion
    {
        // [ADC][T:`S010 Recursive start`]
        public int RecursiveStart(int start)
        {
            return Recursive(start);
        }

        // [ADC][T:`S010 Recursive method`]
        public int Recursive(int number) 
        {
            if(number < 3)
            {
                // [ADC][T:`S010 Call recursive method`]
                number = Recursive(number + 1);
            }

            return number;
        }
    }
}
