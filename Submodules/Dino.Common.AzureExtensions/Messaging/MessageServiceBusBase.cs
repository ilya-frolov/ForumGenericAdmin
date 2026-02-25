using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Dino.Common.Helpers;
using Microsoft.Extensions.Configuration;

namespace Dino.Common.AzureExtensions.Messaging
{
    public abstract class MessageServiceBusBase
    {
        private Dictionary<string, (Type, Delegate)> Commands = new Dictionary<string, (Type, Delegate)>();

        protected abstract string ConnectionStringKey { get; }
        protected abstract string TopicName { get; }

        protected readonly ServiceBusClient _serviceBusClient;
        protected readonly ServiceBusAdministrationClient _serviceBusAdministrationClient;
        protected readonly ServiceBusSender _sender;
        protected readonly ServiceBusProcessor _processor;

        public MessageServiceBusBase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringKey);
            if (connectionString.IsNotNullOrEmpty())
            {
                _serviceBusClient = new ServiceBusClient(connectionString);
                _serviceBusAdministrationClient = new ServiceBusAdministrationClient(connectionString);

                string subscriberName = CreateSubscriberIfNotExist();

                _sender = _serviceBusClient.CreateSender(TopicName);
                _processor = _serviceBusClient.CreateProcessor(TopicName, subscriberName);

                // Register event handlers for processing messages and handling errors
                _processor.ProcessMessageAsync += MessageHandlerAsync;
                _processor.ProcessErrorAsync += ErrorHandlerAsync;

                Commands = GetCommands();
            }
            else
            {
                Console.WriteLine($"Message service connection-string named {ConnectionStringKey} is empty, therefore it wasn't registered.");
            }
        }

        public abstract Dictionary<string, (Type, Delegate)> GetCommands();


        public (Type, Delegate) GetCommand<T>(Func<T, Task> action)
        {
            return (typeof(T), ConvertToDynamicFunc(action));
        }

        private Func<dynamic, Task> ConvertToDynamicFunc<T>(Func<T, Task> action)
        {
            return async (dynamic data) => await action((T)data);
        }

        // Method to send a cache cleaner event
        public async Task SendCommandAsync(string commandName, dynamic commandData)
        {
            if (_sender != null)
            {
                var command = MessageServiceCommand.CreateCommand(commandName, commandData);

                var messageBody = JsonSerializer.Serialize(command);
                var message = new ServiceBusMessage(messageBody);
                await _sender.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"Message service not initialized: {TopicName}, therefore sender couldn't send.");
            }
        }

        protected virtual string GetSubscriberName()
        {
            // WEBSITE_INSTANCE_ID is unique for each instance in Azure: https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings?tabs=kudu%2Cdotnet
            var subscriptionName = Environment.GetEnvironmentVariable("WEBSITE_ROLE_INSTANCE_ID") ?? "localhost_dev";

            return subscriptionName;
        }

        private string CreateSubscriberIfNotExist()
        {
            string subscriptionName = GetSubscriberName();
            if (!_serviceBusAdministrationClient.SubscriptionExistsAsync(TopicName, subscriptionName).Result)
            {
                _serviceBusAdministrationClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(TopicName, subscriptionName)).Wait();
            }

            return subscriptionName;
        }

        private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var command = JsonSerializer.Deserialize<MessageServiceCommand>(body);

            // Check if the dictionary contains this command.Name
            if (Commands.TryGetValue(command.Name, out var actionInfo))
            {
                var (paramType, action) = actionInfo;

                // Deserialize the JSON data into the expected type
                var deserializedData = JsonSerializer.Deserialize(command.JsonData, paramType);

                if (action is Func<object, Task> asyncAction)  // Check if the action is asynchronous
                {
                    // Await the asynchronous action
                    await asyncAction(deserializedData);
                }
                else if (action is Action<object> genericAction)  // Check if it's a synchronous action
                {
                    // Call the synchronous action
                    genericAction(deserializedData);
                }
                else
                {
                    var method = action.GetType().GetMethod("Invoke");

                    // Check if the method is asynchronous
                    if (method.ReturnType == typeof(Task))
                    {
                        var task = (Task)method.Invoke(action, new[] { deserializedData });
                        await task;  // Await the task if it's async
                    }
                    else
                    {
                        // Call the synchronous method
                        method.Invoke(action, new[] { deserializedData });
                    }
                }

            }

            // Complete the message after processing
            await args.CompleteMessageAsync(args.Message);
        }


        // Error handler for message processing errors
        private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Error processing message: {args.Exception}");
            return Task.CompletedTask;
        }


        // Start listening for incoming cache cleaner events
        public async Task StartListeningAsync()
        {
            if (_processor != null)
            {
                await _processor.StartProcessingAsync();
            }
            else
            {
                Console.WriteLine($"Message service not initialized: {TopicName}, therefore listener not started.");
            }
        }

        // Stop listening for events
        public async Task StopListeningAsync()
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync();
            }
            else
            {
                Console.WriteLine($"Message service not initialized: {TopicName}, therefore listener not stopped.");
            }
        }
    }
}
