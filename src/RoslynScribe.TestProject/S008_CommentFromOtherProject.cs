using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    internal class S008_CommentFromOtherProject
    {
        public int S008_LocalMethod(int start)
        {
            // [ADC][S008 Nodes shared comment]
            var logic = new OtherLogic();
            return logic.Multiply(1, 8);
        }
    }
}
