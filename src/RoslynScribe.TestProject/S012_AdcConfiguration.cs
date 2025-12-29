using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S012 This is class`]
    public class S012_AdcConfiguration
    {
        // [ADC][T:`S012 This is class method`]
        public void Method()
        {
            var handler = new Handler();
            var msg = new Message();
            handler.Handle(msg);
            handler.OtherMethod(msg.Value);
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