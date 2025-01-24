namespace RoslynScribe.TestProject
{
    internal class S003_TestClass
    {
        // S003 basicMethod
        public int S003_BasicMethod(int start)
        {
            // S003 basic result
            return start + 1;
        }

        // S003 second method
        public int S003_SecondMethod(int start)
        {
            // S003 call basicMethod
            return S003_BasicMethod(start);
        }
    }
}
