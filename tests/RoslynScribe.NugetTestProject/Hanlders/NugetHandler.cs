namespace RoslynScribe.NugetTestProject.Hanlders
{
    public interface INugetHandler<T>
    {
        public void Handle(T message);
    }
}
