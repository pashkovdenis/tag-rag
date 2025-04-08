using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serina.Semantic.Ai.Pipelines.Filters;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers;
using Serina.Semantic.Ai.Pipelines.SemanticKernel.ServiceSelectors;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Streams;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.TagMemory.Interfaces;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TagRag.Example.Db;
using TagRag.Example.Entity;
using Serina.TagMemory.Plugin;
using Serina.TagMemory.Services;
using Serina.Semantic.Ai.Pipelines.Models;
using System.Diagnostics;
using System.Text;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using Serina.TagMemory;

namespace TagRag.Example.Services
{
    internal class ConversationService : BackgroundService
    {
        private ConversationContext db;
        private IDbConnector dbConnector;

        private const string ModelName = "mistral:latest";
        public const string ollamaHost = "http://192.168.88.105:11434";

        private readonly IPipelineStream Stream = new PipelineChannelStream();

        private readonly IServiceProvider _serviceProvider;


        public ConversationService(ConversationContext db, IDbConnector dbConnector, IServiceProvider serviceProvider)
        {
            this.db = db;
            this.dbConnector = dbConnector;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("TagRag Example"); 

            await LoadData();
            BuildPipeline();

            var pipe = PipelineRegistry.Get("SimpleChat");

            var contextId = Guid.NewGuid();

            var history = new List<RequestMessage>()
            {
                 new RequestMessage("Your task to provide short answer about user conversations. To get details of conversation you can use a memory tool to get information from database just ask tool in natural language to get required information from the database.", MessageRole.System, contextId)
            };
  
            while (true)
            {
                var input = Console.ReadLine();

                history.Add(new RequestMessage(input, MessageRole.User, Guid.NewGuid()));


                var context = new PipelineContext
                {
                    RequestMessage = new RequestMessage("", MessageRole.User, Guid.NewGuid(), History: history.ToArray()),
                    Response = new MessageResponse(),
                    // AutoFunction = true, 
                    EnableFunctions = true, 
                };

                await pipe.ExecuteStepAsync(context, default);

                var response = context.Response.Content;

                history.Add(new RequestMessage(response, MessageRole.Bot, Guid.NewGuid()));

                Console.WriteLine("Bot:" + response);
            }
        }

        // Build pipeline 
        private void BuildPipeline()
        {

            var plugin = new TagMemoryFactory(_serviceProvider)
                .WithMemoryConfig(new Serina.TagMemory.Models.MemoryConfig
                {

                    EngineType = EngineType.Azure,

                    Endpoint = "https://****.openai.azure.com",
                    ModelName = "gpt-4o-mini",

                    Key = "12345",

                    EngineTypeDescription = "this is a sqlite datqabase. The table name is Messages. Table contains columns: Id, Date, User, Context for example to select use: select Id, Date, User, Context from Messages ",

                    Examples = new List<string>
                                        {
                                            "select * from Messages",
                                            "select Id, Date, User, Context from Messages"

                                        },

                    ScanSchema = false,

                })
                .BuildPlugin();


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
                                   } )
                                   .AddFilter(new ClearTextFilter()) 
                                   .AddFilter(new TextChunkerFilter())
                                   .AddReducer(new PairedSlidingWindowReducer())
                                   .WithPlugin(plugin , name : "memory")
                                   .AttachKernel()
                                   .WithName("SimpleChat")
                                   .Build();




        }


        private async Task LoadData()
        {
           
            string csvPath = "conversations.csv"; // Put your CSV file in the same directory as the executable

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
    }
}
