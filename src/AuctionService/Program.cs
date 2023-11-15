using AuctionService;
using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(opt =>
{
  opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")); 
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// configuring Mass Transit 
builder.Services.AddMassTransit(s =>
{
  // Every time an auction is created and sends a message to service buss but if the buss is not available
  // the message sits in Outbox Message Entity and it tries every 10 seconds until the service buss is back
  s.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
  {
    o.QueryDelay = TimeSpan.FromSeconds(10);
    o.UsePostgres(); // uses Postgres DB
    o.UseBusOutbox(); 
  });

  s.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
  s.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

  s.UsingRabbitMq((context, cfg) =>
  {
    cfg.ConfigureEndpoints(context);
  });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

try 
{
  DbInitializer.InitDb(app);
}
catch (Exception ex)
{
  Console.WriteLine(ex);
}

app.Run();
