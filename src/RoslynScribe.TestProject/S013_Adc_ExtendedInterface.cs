using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S013 This is handler test class`]
    internal class S013_Adc_ExtendedInterface
    {
        private readonly IExpandedHandler _iexpandedHandler = new ExpandedHandler();
        private readonly ExpandedHandler _expandedHandler = new ExpandedHandler();

        // [ADC][T:`S013 These are expanded handler invocations`]
        public void ExpandedMethod()
        {
            var msg = new Message();
            _iexpandedHandler.Handle(msg);
            _expandedHandler.Handle(msg);
        }

        public class ExpandedHandler : IExpandedHandler
        {
            public void Handle(object message)
            {
            }

            public int HandleWithResult(object message)
            {
                return 2;
            }

            public void OtherMethod(int value)
            {
            }
        }
    }
}
