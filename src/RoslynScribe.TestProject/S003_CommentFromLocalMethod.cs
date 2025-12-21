namespace RoslynScribe.TestProject
{
    internal class S003_TestClass
    {
        // [ADC][T:`S003 basicMethod`]
        public int S003_BasicMethod(int start)
        {
            // [ADC][T:`S003 basic result`]
            return start + 1;
        }

        // [ADC][T:`S003 second method`]
        public int S003_SecondMethod(int start)
        {
            // [ADC][T:`S003 call basicMethod`]
            return S003_BasicMethod(start);
        }
    }
}
