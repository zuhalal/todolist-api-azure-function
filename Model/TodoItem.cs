using Newtonsoft.Json;

namespace FunctionTodoList.Model
{
    public class TodoItem
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
        [JsonProperty("title")]
        public required string Title { get; set; }
        [JsonProperty("isCompleted")]
        public required bool IsCompleted {  get; set; }
        [JsonProperty("createdAt")]
        public required DateTime CreatedAt { get; set; }
        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
