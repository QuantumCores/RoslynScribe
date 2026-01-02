namespace RoslynScribe.OtherTestProject
{
    public interface IHandler
    {
        void Handle(object message);

        void OtherMethod(int value);
    }

    public interface IExpandedHandler : IHandler
    {
        int HandleWithResult(object message);
    }
}
