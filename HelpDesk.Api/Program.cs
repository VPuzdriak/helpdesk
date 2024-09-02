using System.Text.Json.Serialization;
using HelpDesk.Api.Endpoints;
using HelpDesk.Api.GraphQL;
using HelpDesk.Api.GraphQL.Repositories;
using HelpDesk.Api.Incidents;
using HotChocolate.Data;
using Marten;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddMarten(options =>
    {
        options.Connection(builder.Configuration.GetConnectionString("Events")!);
        options.UseSystemTextJsonForSerialization();

        if (builder.Environment.IsDevelopment())
        {
            options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
        }

        options.Projections.Add<IncidentShortInfoProjection>(ProjectionLifecycle.Inline);
    })
    .AddAsyncDaemon(DaemonMode.Solo)
    .UseLightweightSessions();

builder.Services.AddScoped<IncidentRepository>();

builder.Services
    .AddGraphQLServer()
    .AddMartenFiltering()
    .AddMartenSorting()
    .AddApiTypes()
    .AddGraphQLConventions();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapEndpoints();
app.MapGraphQL();

app.Run();