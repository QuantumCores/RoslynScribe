using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S012 This is handler test class`]
    public class S012_Adc_SimpleInterface
    {
        private readonly IHandler _ihandler = new Handler();
        private readonly Handler _handler = new Handler();


        // [ADC][T:`S012 These are handler invocations`]
        public void Method()
        {
            var msg = new Message();
            _ihandler.Handle(msg);
            _handler.Handle(msg);
        }
    }

    public class Message
    {
        public int Value { get; set; }
    }

    public class Handler : IHandler
    {
        public void Handle(object message)
        {
        }

        public void OtherMethod(int value)
        {
        }
    }
}