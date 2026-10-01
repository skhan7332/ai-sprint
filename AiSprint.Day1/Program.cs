using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient client = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");
// var response = await client.GetResponseAsync("Explain Dependency Injection in one sentence");
// Console.WriteLine(response.Text);

//Exercise 2 history maintain
// List<ChatMessage> history = [new(ChatRole.System, "You are a concise senior .NET mentor.")];
// Console.WriteLine("Ask que: ");
// var input = Console.ReadLine();
// while (input != "exit")
// {
//     history.Add(new(ChatRole.User, input));
//     var reply = new StringBuilder();
//     await foreach (var update in client.GetStreamingResponseAsync(history))
//     {
//         Console.Write(update.Text);
//         reply.Append(update.Text);
//     }
//     history.Add(new(ChatRole.Assistant, reply.ToString()));
//     Console.WriteLine();
//     Console.WriteLine("Ask next que: ");
//     input = Console.ReadLine();
// }

//Exercise 3 Structured output
//add some data in the JobPosting
// foreach (var (rawText, expected) in JobPostingSamples.All)
// {
//     var respose = await client.GetResponseAsync<JobPosting>($"Extract the job details: \n {rawText}");
//     Console.WriteLine($"Expected: {expected}");
//     Console.WriteLine(respose.TryGetResult(out var actual) ? $"Actial: {actual}" : "Actual: <invalid JSON>");
//     Console.WriteLine(respose.TryGetResult(out var remote) ? $"Actial is remote: {actual?.IsRemote}" : "Actual remote: <invalid JSON>");
//     Console.WriteLine();    
// }

//Exercise 4: Function/tool calling
IChatClient toolClient = new ChatClientBuilder(client).UseFunctionInvocation().Build();
var options = new ChatOptions
{
    Tools = [
        AIFunctionFactory.Create(GetOrderStatus),
        AIFunctionFactory.Create(GetCustomerName),
        ]
};
var r = await toolClient.GetResponseAsync("Where is order 42", options);
Console.WriteLine($"Result: {r.Text}");

var c = await toolClient.GetResponseAsync("Who is customer 12", options);
Console.WriteLine($"Customer result: {c.Text}");

[Description("Gets the shipping status of an order by its id")]
static string GetOrderStatus(int orderId) => orderId == 42 ? "Shipped yesterday" : "not found";

[Description("Gets the customer nameby its id")]
static string GetCustomerName(int customerId)
{
    switch (customerId)
    {
        case 1:
            return "John";

        case 10:
            return "Mathew";

        default:
            return "not found";
    }
}

// // See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
