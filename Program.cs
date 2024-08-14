using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var credential = new DefaultAzureCredential();
        var armClient = new ArmClient(credential);

        // Get all subscriptions
        var subscriptions = new List<SubscriptionResource>();
        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(subscription);
        }

        Console.WriteLine("Select subscriptions (comma-separated indices) select none to use all:");
        for (int i = 0; i < subscriptions.Count; i++)
        {
            Console.WriteLine($"{i}: {subscriptions[i].Data.DisplayName}");
        }
        var userInput = Console.ReadLine();
        var selectedSubscriptions = string.IsNullOrWhiteSpace(userInput)
            ? subscriptions
            : userInput.Split(',').Select(int.Parse).Select(index => subscriptions[index]).ToList();

        var resources = new List<GenericResourceData>();

        foreach (var subscription in selectedSubscriptions)
        {
            var subscriptionClient = armClient.GetSubscriptionResource(subscription.Data.Id);
            await foreach (var rg in subscriptionClient.GetResourceGroups().GetAllAsync())
            {
                foreach (var resource in rg.GetGenericResources())
                {
                    resources.Add(resource.Data);
                }
            }
        }

        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions { WriteIndented = false });
        await System.IO.File.WriteAllTextAsync("resources.json", json);

        Console.WriteLine(resources.Count() + " resources from "+ selectedSubscriptions.Count() +" subscriptions have been written to resources.json");
    }
}
