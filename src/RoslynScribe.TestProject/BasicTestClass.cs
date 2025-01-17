namespace RoslynScribe.TestProject
{
    // This is BasicTestClass
    public class BasicTestClass
    {
        // This is BasicMethod
        public int BasicMethod(int start)
        {
            // This is result
            return start + 1;
        }

        // This is multiline comment
        // This is return addition
        public int SecondMethod(int start)
        {
            var end = BasicMethod(start);

            // This is Nodes shared comment get logic data 
            var logic= new Logic();
            return logic.Add(end, 8);            
        }
    }
}
