namespace RoslynScribe.TestProject
{
    internal class S006_ThirdPartyLibrary
    {
        internal string S006_CallThirdPartyLibrary()
        {
            var date = DateTime.Now;
            // [ADC][T:`S006 call external method to add days`]
            date = date.AddDays(1);

            return date.ToString();
        }
    }
}
