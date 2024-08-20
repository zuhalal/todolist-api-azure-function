using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace FunctionTodoList
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private static ArrayList arr = [];

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("AddTodoList")]
        public async Task<IActionResult> RunAdd([HttpTrigger(AuthorizationLevel.Function, "post", Route = "todos/add")] HttpRequest req)
        {
            _logger.LogInformation("ToDo List HTTP trigger function processed a post request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] != null && arr[i].ToString().Equals(requestBody))
                {
                    return new BadRequestObjectResult("Duplicate Todo List Found: " + requestBody);
                }
            }
            
            arr.Add(requestBody);
            return new OkObjectResult("Todo List Added: " + requestBody);
        }

        [Function("GetTodoList")]
        public IActionResult RunGet([HttpTrigger(AuthorizationLevel.Function, "get", Route = "todos")] HttpRequest req)
        {
            _logger.LogInformation("ToDo List HTTP trigger function processed a get request.");
            return new OkObjectResult(arr);
        }

        [Function("UpdateTodoList")]
        public async Task<IActionResult> RunUpdate([HttpTrigger(AuthorizationLevel.Function, "put", Route = "todos/update/{name}")] HttpRequest req, string name)
        {
            _logger.LogInformation("ToDo List HTTP trigger function processed a put request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            int? idx = null;
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] != null && arr[i].ToString().Equals(name))
                {
                    idx = i; 
                    break;
                }
            }

            if (idx.HasValue) {
                arr[idx.Value] = requestBody;
                return new OkObjectResult("Todo List updated from: " + name +  ", to: " + requestBody);
            }
            return new NoContentResult();
        }

        [Function("DeleteTodoList")]
        public async Task<IActionResult> RunDelete([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "todos/delete/{name}")] HttpRequest req, string name)
        {
            _logger.LogInformation("ToDo List HTTP trigger function processed a delete request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            int? idx = null;
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] != null && arr[i].ToString().Equals(name))
                {
                    idx = i;
                    break;
                }
            }

            if (idx.HasValue)
            {
                arr.RemoveAt(idx.Value);
                return new OkObjectResult("Todo List removed: " + name);
            }
            return new NoContentResult();
        }
    }
}
