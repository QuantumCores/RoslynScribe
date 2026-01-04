using Microsoft.AspNetCore.Mvc;

namespace RoslynScribe.TestProject
{
    [Route("api/v1/[controller]")]
    internal class S017_Adc_NugetBaseClass : ControllerBase
    {
        const string BaseRoute = "test";
        const string Load2Route = BaseRoute + "/Load2";

        [HttpPost("Load")]
        public async void LoadSomething([FromBody] int loadSize, [FromBody] int timeLimit)
        {
        }

        [HttpPost(Load2Route)]
        public async void LoadSomething([FromBody] int loadSize)
        {
        }
    }
}
