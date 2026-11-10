
using BikeRepairAI.Data;
using BikeRepairAI.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DBConnection")));

// OpenAI client - Embeddings
builder.Services.AddHttpClient("openai", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            builder.Configuration["OpenAI:ApiKey"]);
});

builder.Services.AddHttpClient("HuggingFace", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

// Groq client - Chat
builder.Services.AddHttpClient("groq", client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            builder.Configuration["Groq:ApiKey"]);
});

// Application services
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<RagService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://bike-repair-ui.nileshpatil7476.workers.dev")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AngularFrontend");

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    Application = "BikeRepairAI",
    Status = "Running",
    Message = "BikeRepairAI API is working!"
}));

app.MapControllers();

app.Run();
