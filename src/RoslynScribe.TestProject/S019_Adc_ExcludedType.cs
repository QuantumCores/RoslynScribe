namespace RoslynScribe.TestProject
{
    internal class S019_Adc_ExcludedType
    {
    }

    public class IncludedType
    {
        public static void DoWork()
        {
            ExcludedType.DoWork();
        }
    }

    public class ExcludedType
    {
        public static void DoWork() { }
    }
}
