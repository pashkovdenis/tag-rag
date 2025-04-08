Building Smarter RAG Systems with Semantic Kernel and SQL Memory
When it comes to Retrieval-Augmented Generation (RAG), there are several ways to provide external information to a chatbot.

The most straightforward method — useful for a small set of known facts — is to inject them directly into the system prompt. For example:

plaintext
Копировать
Редактировать
###Context###
Username: James  
UserEmail: test@gmail.com
But what happens when we have more data?

The Vector Database Approach (And Its Limits)
The classic approach is using a vector database. It stores information as vector embeddings, and the LLM retrieves the most relevant chunks by searching for semantic similarity.

It’s fine for basic use cases, but quickly hits bottlenecks:

The vector database isn’t smart — it only ranks by similarity. It doesn’t know that The Matrix and The Lord of the Rings are different genres, or even that they’re movies at all.

Context blindness — retrieved chunks lack broader meaning, which can lead to irrelevant mashups or hallucinations.

This is why naive RAG implementations often do more harm than good. Let’s say you want to analyze a long customer support transcript (which exceeds the model’s context window). You want to answer questions like:

How long were the delays between responses?

Was the issue resolved?

Were there any unanswered questions?

A vector DB alone can’t give you that level of structured insight.

Knowledge Graphs Help, But…
A knowledge graph can help extract entity relationships, which is useful. But it doesn’t give you full reports or behavioral analysis like:

Longest delay in replies

Open vs resolved issues

Agent performance metrics

Also, there’s no universal knowledge graph — you need to build it per use case. In this case: conversation analysis.

Introducing the TagRAG Plugin for Semantic Kernel
In this post, I’ll walk you through using the TagMemory plugin for Semantic Kernel to query structured data and maintain conversation history — enabling smarter and deeper insights.

Let’s build it step by step in C#.

Step 1: Create a Console App
First, install the required packages:

bash
Копировать
Редактировать
dotnet add package Serina.TagMemory.Plugin
dotnet add package Serina.Semantic.Ai.Pipelines.SemanticKernel
Step 2: Prepare the Data
We'll simulate a long conversation transcript (e.g., from tech support) using a CSV file with at least 500 rows in this format:
date,user,message

Load the data into an in-memory SQLite database:

csharp
Копировать
Редактировать
private async Task LoadData()
{
    string csvPath = "conversations.csv";

    foreach (var line in File.ReadLines(csvPath).Skip(1)) // skip header
    {
        var parts = line.Split(',', 3);
        if (parts.Length != 3) continue;

        var message = new Message
        {
            Date = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            User = parts[1],
            Content = parts[2]
        };

        db.Messages.Add(message);
    }

    db.SaveChanges();
}
Step 3: Configure the Pipeline
Here’s how we configure the pipeline with the TagMemory plugin and Semantic Kernel:

csharp
Копировать
Редактировать
var plugin = new TagMemoryFactory(_serviceProvider)
    .WithMemoryConfig(new MemoryConfig
    {
        EngineType = EngineType.Azure,
        Endpoint = "https://****.openai.azure.com",
        ModelName = "gpt-4o-mini",
        Key = "12345",
        EngineTypeDescription = "SQLite database. Table: Messages (Id, Date, User, Context). Example query: select Id, Date, User, Context from Messages.",
        Examples = new List<string>
        {
            "select * from Messages",
            "select Id, Date, User, Context from Messages"
        },
        ScanSchema = false,
    })
    .BuildPlugin();
Then wire up the pipeline:

csharp
Копировать
Редактировать
PipelineBuilder.New()
    .SetNext(new SimpleChatStep())
    .WithKernel(new SemanticKernelOptions
    {
        Models = new List<SemanticModelOption>
        {
            new SemanticModelOption
            {
                Endpoint = "https://***.openai.azure.com",
                Name = "gpt-4o-mini",
                Key = "123456",
                EngineType = 2
            }
        }
    })
    .AddFilter(new ClearTextFilter())
    .AddFilter(new TextChunkerFilter())
    .AddReducer(new PairedSlidingWindowReducer())
    .WithPlugin(plugin, name: "memory")
    .AttachKernel()
    .WithName("SimpleChat")
    .Build();
Step 4: Running the Bot
Now we run the loop and let the model interact with our SQL memory:

csharp
Копировать
Редактировать
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    Console.WriteLine("TagRAG Example");

    await LoadData();
    BuildPipeline();

    var pipe = PipelineRegistry.Get("SimpleChat");
    var contextId = Guid.NewGuid();

    var history = new List<RequestMessage>
    {
        new("Your task is to provide a short answer about user conversations. To get details, use the memory tool to query the database in natural language.", MessageRole.System, contextId)
    };

    while (true)
    {
        var input = Console.ReadLine();
        history.Add(new RequestMessage(input, MessageRole.User, Guid.NewGuid()));

        var context = new PipelineContext
        {
            RequestMessage = new RequestMessage("", MessageRole.User, Guid.NewGuid(), History: history.ToArray()),
            Response = new MessageResponse(),
            EnableFunctions = true,
        };

        await pipe.ExecuteStepAsync(context, default);

        var response = context.Response.Content;
        history.Add(new RequestMessage(response, MessageRole.Bot, Guid.NewGuid()));

        Console.WriteLine("Bot: " + response);
    }
}
Here, we instruct the model to use the memory tool to retrieve SQL data based on natural language questions.

What You Can Analyze
Once everything’s wired up, you can start asking questions like:

How many messages were exchanged?

Which users participated?

What issues were discussed?

Was the issue resolved?

What was the longest delay between replies?

💡 The more detailed your system prompt, the more accurate your results.
Example: "To determine if the issue is resolved, analyze the last N messages."

The TagMemory plugin uses Polly under the hood to retry SQL calls and auto-adjust queries on failure.
