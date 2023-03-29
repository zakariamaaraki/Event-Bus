using Service_bus.Services;
using Service_bus.Models;
using Service_bus.Configurations;
using Service_bus.Middlewares;
using Service_bus.Volumes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventOptions>(
    builder.Configuration.GetSection("EventOptions"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventLogger<Event>, EventLogger<Event>>();
builder.Services.AddSingleton<IEventDispatcher<Event>, EventDispatcher<Event>>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<IEventsLoader, EventsLoader>();

builder.Services.AddSingleton<IMiddleware, ExceptionMiddleware>();

builder.Services.AddHostedService<EventProcessor>();

var app = builder.Build();

app.UseMiddleware<IMiddleware>();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();