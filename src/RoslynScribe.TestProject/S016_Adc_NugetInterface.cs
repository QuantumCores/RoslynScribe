using RoslynScribe.NugetTestProject.Hanlders;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S016 This is nuget test class without invocations`]
    internal class S016_Adc_NugetInterface
    {
        //private readonly INugetHandler<Message> _ihandler = new NugetHandler<Message>();
        //private readonly NugetHandler<Message> _handler = new NugetHandler<Message>();  
    }

    internal class NugetHandler<T> : INugetHandler<T>
    {
        public virtual void Handle(T message)
        {
        }
    }

    internal class NugetMessageHandler : NugetHandler<NugetMessage>
    {
        public override void Handle(NugetMessage message)
        {
        }
    }

    internal class NugetMessage
    {
    }
}
