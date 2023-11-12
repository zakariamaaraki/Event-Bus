using Service_bus.Services;
using Service_bus.Configurations;
using Service_bus.Middlewares;
using Service_bus.Filters;
using Service_bus.Volumes;
using Service_bus.LeaderElection;
using Service_bus.Consul;
using Consul;
using Service_bus.ServiceRegistry;
using Service_bus.DataReplication;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<EventOptions>(
    builder.Configuration.GetSection("EventOptions"));

builder.Services.Configure<ConsulOptions>(
    builder.Configuration.GetSection("ConsulOptions"));

// Consul
builder.Services.AddSingleton<IHostedService, ConsulHostedService>();
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    var address = builder.Configuration["ConsulOptions:ConnectionString"];
    consulConfig.Address = new Uri(address);
}));
builder.Services.AddSingleton<Func<IConsulClient>>(p => () => new ConsulClient(consulConfig =>
{
    var address = builder.Configuration["ConsulOptions:ConnectionString"];
    consulConfig.Address = new Uri(address);
}));

builder.Services.AddControllers(options =>
{
    //options.Filters.Add<LeaderElectionChecker>();
});

// Http Client
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventLogger<Service_bus.Models.Event>, EventLogger<Service_bus.Models.Event>>();
builder.Services.AddSingleton<IEventDispatcher<Service_bus.Models.Event>, EventDispatcher<Service_bus.Models.Event>>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<IEventsLoader, EventsLoader>();
builder.Services.AddSingleton<LeaderElectionChecker, LeaderElectionChecker>();

// Leader Election
builder.Services.AddSingleton<ILeaderElectionClient, ConsulLeaderElection>();

// Service Registry
builder.Services.AddSingleton<IServiceBusRegistry, ServiceBusRegistry>();

// Data Replication
builder.Services.AddSingleton<ILeaderToFollowersDataReplication, LeaderToFollowersDataReplication>();

builder.Services.AddSingleton<IMiddleware, ExceptionMiddleware>();

builder.Services.AddHostedService<EventProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<IMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();


app.Run();