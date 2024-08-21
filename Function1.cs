using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections;
using Newtonsoft.Json;
using FunctionTodoList.Model;
using Microsoft.Azure.Cosmos.Linq;

namespace FunctionTodoList
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private static string CONNECTION_STRING = Environment.GetEnvironmentVariable("CosmosDbConnection") ?? "";
        private static string _DATABASE = "ToDoList";
        private static string _CONTAINER = "ToDoItem";

        private static CosmosClient client = new CosmosClient(CONNECTION_STRING);
        private Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);
        
        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("AddTodoList")]
        public async Task<IActionResult> RunAdd([HttpTrigger(AuthorizationLevel.Function, "post", Route = "todos/add")] HttpRequest req)
        {
           try
            {
                _logger.LogInformation("ToDo List HTTP trigger function processed a post request.");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var todoItemData = JsonConvert.DeserializeObject<Model.TodoItem>(requestBody);
                todoItemData.Id = Guid.NewGuid().ToString();
                todoItemData.CreatedAt = DateTime.Now;
                todoItemData.IsCompleted = false;

                var todoItem = await cosmosContainer.CreateItemAsync(todoItemData);
                return new OkObjectResult("Todo List Added: " + todoItem.Resource.ToString());
            } catch (CosmosException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("GetTodoList")]
        public async Task<IActionResult> RunGet([HttpTrigger(AuthorizationLevel.Function, "get", Route = "todos")] HttpRequest req)
        {
            _logger.LogInformation("ToDo List HTTP trigger function processed a get request.");

            List<TodoItem> todoItems = new List<TodoItem>();

            var linqQueryable = cosmosContainer.GetItemLinqQueryable<TodoItem>();
            var iterator = linqQueryable.ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                foreach (var item in await iterator.ReadNextAsync())
                {
                     todoItems.Add(item);
                }
            }
            return new OkObjectResult(todoItems);
        }

        [Function("GetSpecificTodoList")]
        public async Task<IActionResult> RunGetOne([HttpTrigger(AuthorizationLevel.Function, "get", Route = "todos/{id}")] HttpRequest req, string id)
        {
           try
            {
                _logger.LogInformation("ToDo List HTTP trigger function processed a get request.");

                List<TodoItem> todoItems = new List<TodoItem>();

                var linqQueryable = cosmosContainer.GetItemLinqQueryable<TodoItem>().Where(p => p.Id.Equals(id));
                var iterator = linqQueryable.ToFeedIterator();

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var item = response.FirstOrDefault();

                    if (item != null)
                    {
                        return new OkObjectResult(item);
                    }
                }

                return new NotFoundResult();
            }
            catch (CosmosException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            } catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("UpdateTodoList")]
        public async Task<IActionResult> RunUpdate([HttpTrigger(AuthorizationLevel.Function, "put", Route = "todos/update/{id}")] HttpRequest req, string id)
        {
            try
            {
                _logger.LogInformation("ToDo List HTTP trigger function processed a put request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var todoItemData = JsonConvert.DeserializeObject<TodoItem>(requestBody);

                ItemResponse<TodoItem> findResponse = await cosmosContainer.ReadItemAsync<TodoItem>(id, new PartitionKey(id));
                var itemToUpdate = findResponse.Resource;

                itemToUpdate.Title = todoItemData.Title;
                itemToUpdate.IsCompleted = todoItemData.IsCompleted ? todoItemData.IsCompleted : false;
                itemToUpdate.UpdatedAt = DateTime.Now;
                
                ItemResponse<TodoItem> updateResponse = await cosmosContainer.ReplaceItemAsync<TodoItem>(itemToUpdate, id, new PartitionKey(id));

                return new OkObjectResult($"Item updated successfully: {updateResponse.Resource.Id}");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
        }

        [Function("DeleteTodoList")]
        public async Task<IActionResult> RunDelete([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "todos/delete/{id}")] HttpRequest req, string id)
        {
            try
            {
                _logger.LogInformation("ToDo List HTTP trigger function processed a delete request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var todoItemData = JsonConvert.DeserializeObject<TodoItem>(requestBody);

                ItemResponse<TodoItem> findResponse = await cosmosContainer.ReadItemAsync<TodoItem>(id, new PartitionKey(id));
                var itemToDelete = findResponse.Resource;

                ItemResponse<TodoItem> deleteResponse = await cosmosContainer.DeleteItemAsync<TodoItem>(id, new PartitionKey(id));
                return new OkObjectResult("Item with ID: " + id + " deleted successfully ");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
        }
    }
}
