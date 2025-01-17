namespace RoslynScribe.TestProject
{
    internal class S003_CommentFromLocalMethod
    {
        public int S003_BasicMethod(int start)
        {
            // S003 basic result
            return start + 1;
        }

        // S003 second method
        public int S003_SecondMethod(int start)
        {
            // S003 call basic method
            return S003_BasicMethod(start);
        }
    }
}
