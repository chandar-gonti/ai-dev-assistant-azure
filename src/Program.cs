using AiDevAssistant.Services;
using Azure.AI.ContentSafety;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Azure.Cosmos;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// Use Managed Identity in Azure, DefaultAzureCredential locally
var credential = new DefaultAzureCredential();

// Azure OpenAI
builder.Services.AddSingleton(_ =>
    new OpenAIClient(
        new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!),
        credential));

// Azure AI Search
builder.Services.AddSingleton(_ =>
    new SearchClient(
        new Uri(builder.Configuration["AzureAISearch:Endpoint"]!),
        builder.Configuration["AzureAISearch:IndexName"]!,
        credential));

// Cosmos DB
builder.Services.AddSingleton(_ =>
    new CosmosClient(
        builder.Configuration["CosmosDb:Endpoint"],
        credential));

// Content Safety
builder.Services.AddSingleton(_ =>
    new ContentSafetyClient(
        new Uri(builder.Configuration["ContentSafety:Endpoint"]!),
        credential));

// Domain services
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IContentSafetyService, ContentSafetyService>();
builder.Services.AddScoped<IAssistantService, AssistantService>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();