namespace RoslynScribe.TestProject
{
    internal class S005_IfStatementBlockComment
    {
        internal int S005_IfStatementBlock(string condition)
        {
            // [ADC][Check condition value]
            if (condition == "one")
            {
                // [ADC][S005 If condition is one then 1]
                return 1;
            }
            else if (condition == "two")
            {
                // [ADC][S005 If condition is two then 2]
                return 2; 
            }
            else
            {
                // [ADC][S005 If condition is other then -1]
                return -1;
            }
        }
    }
}
