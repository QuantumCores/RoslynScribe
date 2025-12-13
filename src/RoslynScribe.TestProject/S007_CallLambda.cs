namespace RoslynScribe.TestProject
{
    internal class S007_CallLambda
    {
        internal void S007_CallLambdaExpression()
        {
            // [ADC][S007 prepare lambda]
            var colleaction = Enumerable.Range(0, 10);
            var result = colleaction.Select(x =>
            {
                // [ADC][S007 add one in lambda expression]
                var y = x + 1;
                return Math.Pow(y, 2);
            });
        }
    }
}
