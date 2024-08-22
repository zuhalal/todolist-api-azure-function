using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionTodoList.Model
{
    public class Reminder
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("TodoListId")] 
        public string TodoListId { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set; }
        [JsonProperty("createdAt")]
        public required DateTime CreatedAt { get; set; }
        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
