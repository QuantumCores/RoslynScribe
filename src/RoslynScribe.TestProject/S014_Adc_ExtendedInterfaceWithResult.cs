using RoslynScribe.OtherTestProject;

namespace RoslynScribe.TestProject
{
    // [ADC][T:`S014 This is handler test class`]
    internal class S014_Adc_ExtendedInterfaceWithResult
    {
        private readonly IExpandedHandler _iexpandedHandler = new ExpandedHandler();
        private readonly ExpandedHandler _expandedHandler = new ExpandedHandler();

        // [ADC][T:`S014 These are expanded handler with result invocations`]
        public void ExpandedMethodWithResult()
        {
            var msg = new Message();
            var ires = _iexpandedHandler.HandleWithResult(msg);
            var res = _expandedHandler.HandleWithResult(msg);
        }

        public class ExpandedHandler : IExpandedHandler
        {
            public void Handle(object message)
            {
            }

            public int HandleWithResult(object message)
            {
                return 3;
            }

            public void OtherMethod(int value)
            {
            }
        }
    }
}
