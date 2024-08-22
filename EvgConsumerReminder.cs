// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using FunctionTodoList.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionTodoList
{
    public class EvgConsumerReminder
    {
        private readonly ILogger<EvgConsumerReminder> _logger;
        private static string CONNECTION_STRING = Environment.GetEnvironmentVariable("CosmosDbConnection") ?? "";
        private static string _DATABASE = "Reminder";
        private static string _CONTAINER = "reminder";

        public EvgConsumerReminder(ILogger<EvgConsumerReminder> logger)
        {
            _logger = logger;
        }

        [Function(nameof(EvgConsumerReminder))]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridData)
        {
            var subject = eventGridData.Subject;
            var todoItemData = JsonConvert.DeserializeObject<TodoItem>(eventGridData.Data.ToString());
            var id = eventGridData.Id;

            CosmosClient client = new CosmosClient(CONNECTION_STRING);
            Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

            var currentData = cosmosContainer.GetItemLinqQueryable<Reminder>(true).Where(p => p.TodoListId.Equals(todoItemData.Id)).AsEnumerable().FirstOrDefault();

            if (currentData == null && subject.Equals("Create/*"))
            {
                try
                {
                    var reminder = new Reminder()
                    {
                        Id = Guid.NewGuid().ToString(),
                        IsCompleted = todoItemData.IsCompleted,
                        Message = todoItemData.Title,
                        TodoListId = todoItemData.Id,
                        CreatedAt = DateTime.Now,
                    };
                    var reminderItem = await cosmosContainer.CreateItemAsync(reminder);
                }
                catch (Exception ex)
                {
                    _logger.LogError("An error in reminder consumer occurred: " + ex.ToString());

                }
            } else if (currentData != null && subject.Equals("Update/*")) 
            {
                try
                {
                    currentData.IsCompleted = todoItemData.IsCompleted;
                    if (todoItemData.Title != null)
                    {
                        currentData.Message = todoItemData.Title;
                    }
                    ItemResponse<Reminder> updateResponse = await cosmosContainer.ReplaceItemAsync<Reminder>(currentData, currentData.Id, new PartitionKey(currentData.Id));
                }
                catch (Exception ex) {
                    _logger.LogError("An error in reminder consumer occurred: " + ex.ToString());
                }
            }
        }
    }
}
