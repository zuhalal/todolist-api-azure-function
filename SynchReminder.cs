using System;
using System.Text;
using Azure.Messaging.EventHubs;
using FunctionTodoList.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionTodoList
{
    public class SynchReminder
    {
        private readonly ILogger<SynchReminder> _logger;

        private static string CONNECTION_STRING = Environment.GetEnvironmentVariable("CosmosDbConnection") ?? "";
        private static string _DATABASE = "Reminder";
        private static string _CONTAINER = "reminder";

        private static CosmosClient client = new CosmosClient(CONNECTION_STRING);
        private Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

        public SynchReminder(ILogger<SynchReminder> logger)
        {
            _logger = logger;
        }

        [Function(nameof(SynchReminder))]
        public async Task Run([EventHubTrigger("reminder", Connection = "Evh-pdpzuhal-listen", ConsumerGroup = "log-sync-reminder")] EventData[] events)
        {
            var exceptions = new List<Exception>();
            foreach (EventData @event in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(@event.EventBody.ToArray());
                    var todoItemData = JsonConvert.DeserializeObject<TodoItem>(messageBody);

                    var itemToUpdate = cosmosContainer.GetItemLinqQueryable<Reminder>(true).Where(p => p.TodoListId.Equals(todoItemData.Id)).AsEnumerable().FirstOrDefault(); Console.WriteLine("Masook");
                    if (itemToUpdate != null) {
                        if (todoItemData.Title != null)
                        {
                            itemToUpdate.Message = todoItemData.Title ?? itemToUpdate.Message;
                        }
                        itemToUpdate.UpdatedAt = DateTime.Now;
                        itemToUpdate.IsCompleted = todoItemData.IsCompleted;
                        ItemResponse<Reminder> updateResponse = await cosmosContainer.ReplaceItemAsync<Reminder>(itemToUpdate, itemToUpdate.Id, new PartitionKey(itemToUpdate.Id));
                    } else
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
                }
                catch (Exception ex) {
                    Console.WriteLine(" bbb " + ex.ToString());

                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 1) {
                throw new AggregateException(exceptions);
            } else if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }
        }
    }
}
