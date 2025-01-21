using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynScribe.Domain.ScribeConsole;
using System;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    public class TestFixture : IDisposable
    {
        public static bool _isLoaded = false;
        public static object _lock = new object();
        private static MSBuildWorkspace _workspace;
        private static Solution _solution;

        public static async Task Prepare()
        {
            if (!_isLoaded)
            {
                lock (_lock)
                {
                    if (!_isLoaded)
                    {
                        _workspace = SolutionProvider.GetWorkspace();
                    }
                }

                var solutionPath = "D:\\Source\\RoslynScribe\\RoslynScribe.sln";
                _solution = await SolutionProvider.GetSolution(_workspace, solutionPath);
            }
        }

        internal static MSBuildWorkspace GetWorkspace()
        {
            return _workspace;
        }

        internal static Solution GetSolution()
        {
            return _solution;
        }

        public void Dispose()
        {
            _workspace?.Dispose();
            _solution = null;
        }
    }
}