namespace RoslynScribe.TestProject
{
    internal class S005_IfStatementBlockComment
    {
        internal int S005_IfStatementBlock(string condition)
        {
            // Check condition value
            if (condition == "one")
            {
                // S005 If condition is one then 1
                return 1;
            }
            else if (condition == "two")
            {
                // S005 If condition is two then 2
                return 2; 
            }
            else
            {
                // S005 If condition is other then -1
                return -1;
            }
        }
    }
}
