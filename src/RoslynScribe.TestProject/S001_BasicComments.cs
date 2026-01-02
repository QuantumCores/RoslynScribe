// [ADC][T:`S001 namespace comment`]
namespace RoslynScribe.TestProject
{
    // [ADC][T:`S001 BasicTestClass`]
    public class S001_BasicComments
    {
        // [ADC][T:`S001 BasicMethod`]
        public int S001_BasicMethod(int start)
        {
            // [ADC][T:`S001 result`]
            return start + 1;
        }

        public int ZZZ_MethodShouldNotOverWriteNodeMetaInfo(int start)
        {
            return start + 2;
        }
    }
}
