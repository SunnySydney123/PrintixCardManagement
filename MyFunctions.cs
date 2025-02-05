using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PrintixCardManagement
{
    public class MyFunctions
    {
        private readonly ILogger<MyFunctions> _logger;

        public MyFunctions(ILogger<MyFunctions> logger)
        {
            _logger = logger;
        }

        [Function("PrintixFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
