using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S015 This is handler test class`]
    internal class S015_Adc_GenericInterface
    {
        private readonly IGenericHandler<Message> _ihandler = new GenericHandler<Message>();
        private readonly GenericHandler<Message> _handler = new GenericHandler<Message>();

        // [ADC][T:`S015 These are handler invocations`]
        public void Method()
        {
            var msg = new Message();
            _ihandler.Handle(msg);
            _handler.Handle(msg);
        }
    }

    public class GenericHandler<T> : IGenericHandler<T>
    {
        public void Handle(T message)
        {
        }
    }
}
