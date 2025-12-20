namespace RoslynScribe.TestProject
{
    // [ADC][T:`S011 This is class`]
    internal class S011_InterfaceImpl : IMyInterface
    {
        // [ADC][T:`S011 This is class property`]
        public int Value { get; set; }

        // [ADC][T:`S011 This is class method`]
        public int GetValue()
        {
            return Value;
        }
    }

    // [ADC][T:`S011 This is interface`]
    public interface IMyInterface
    {
        // [ADC][T:`S011 This is interface property`]
        int Value { get; set; }

        // [ADC][T:`S011 This is interface method`]
        int GetValue();
    }
}
