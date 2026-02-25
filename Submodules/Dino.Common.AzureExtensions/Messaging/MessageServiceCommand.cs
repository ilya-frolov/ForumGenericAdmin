using System.Text.Json;

namespace Dino.Common.AzureExtensions.Messaging
{
    public class MessageServiceCommand
    {
        public MessageServiceCommand(string name, string jsonData)
        {
            Name = name;
            JsonData = jsonData;
        }

        public static MessageServiceCommand CreateCommand(string name, dynamic jsonData)     // Will automatically convert the object to json.
        {
            return new MessageServiceCommand(name, JsonSerializer.Serialize(jsonData));
        }

        public string Name { get; set; }
        public string JsonData { get; set; }
    }
}
