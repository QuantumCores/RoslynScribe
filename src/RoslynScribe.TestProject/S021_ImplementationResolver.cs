namespace RoslynScribe.TestProject
{
    public interface IMultiResolverHandler
    {
        void Handle();
    }

    public class MultiResolverHandlerA : IMultiResolverHandler
    {
        public void Handle()
        {
        }
    }

    public class MultiResolverHandlerB : IMultiResolverHandler
    {
        public void Handle()
        {
        }
    }

    public class S021_ImplementationResolver_Config
    {
        private readonly IMultiResolverHandler _handler;

        public S021_ImplementationResolver_Config(IMultiResolverHandler handler)
        {
            _handler = handler;
        }

        public void Run()
        {
            _handler.Handle();
        }
    }

    public class S022_ImplementationResolver_Assignment
    {
        private readonly IMultiResolverHandler _handler;

        public S022_ImplementationResolver_Assignment()
        {
            _handler = CreateHandler();
        }

        private static MultiResolverHandlerA CreateHandler()
        {
            return new MultiResolverHandlerA();
        }

        public void Run()
        {
            _handler.Handle();
        }
    }

    public interface IUniqueResolverHandler
    {
        void Handle();
    }

    public class UniqueResolverHandler : IUniqueResolverHandler
    {
        public void Handle()
        {
        }
    }

    public class S023_ImplementationResolver_Unique
    {
        public void Run(IUniqueResolverHandler handler)
        {
            handler.Handle();
        }
    }
}
