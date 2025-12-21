namespace RoslynScribe.TestProject
{
    /// <summary>
    /// Here we check if call A -> D won't brake B -> D due to recursion guards
    /// </summary>
    internal class S009_TwoMethodCalls
    {
        // [ADC][T:`S009 Method A calls C`]
        public int S009_Method_A(int start)
        {
            return S009_Method_C(start + 1);
        }

        // [ADC][T:`S009 Method B calls C`]
        public int S009_Method_B(int start)
        {
            return S009_Method_C(start - 1);
        }

        public int S009_Method_C(int start)
        {
            return S009_Method_D(start);
        }

        // [ADC][T:`S009 Method D called by C`]
        public int S009_Method_D(int start)
        {
            return start + 1;
        }
    }
}
