namespace RoslynScribe.TestProject
{
    internal class S004_TestClass
    {
        // [ADC][T:`S004 return addition`]
        public int S004_SecondMethod(int start)
        {
            // [ADC][T:`S004 Nodes shared comment get logic data`]
            var logic = new Logic();
            return logic.Add(1, 8);
        }
    }
}
