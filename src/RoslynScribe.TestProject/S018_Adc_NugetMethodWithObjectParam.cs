using RoslynScribe.NugetTestProject.Senders;

namespace RoslynScribe.TestProject
{
    internal class S018_Adc_NugetMethodWithObjectParam
    {
        INugetSender _sender;

        public void Method()
        {
            _sender.Send(new NugetMessage());
        }
    }
}
