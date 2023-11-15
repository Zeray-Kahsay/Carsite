using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvHttpClient>().AddPolicyHandler(GetPolicy());

// configuring Mass Transit 
builder.Services.AddMassTransit(s =>
{
  s.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

  s.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
  
  s.UsingRabbitMq((context, cfg) =>
  {
    // configuring based on end-point i.e search-auction-created 
    cfg.ReceiveEndpoint("search-auction-created", e =>
    {
      e.UseMessageRetry(r => r.Interval(5, 5));
      e.ConfigureConsumer<AuctionCreatedConsumer>(context);
    });

    // configuring based on the whole consumer, i.e AuctionCreatedConsumer
    cfg.ConfigureEndpoints(context);
  });
});


var app = builder.Build();

// Configure the HTTP request pipeline.



app.UseAuthorization();

app.MapControllers();

// adds resiliency:- though auction service is down, search service starts
app.Lifetime.ApplicationStarted.Register(async () =>
{
  try
  {
    await DbInitializer.InitDb(app);
  }
  catch (Exception e)
  {
    Console.WriteLine(e);
  }
});


app.Run();


// In case the AuctionService is down, AuctionSvHttpClient tries until auction service is back every 3 seconds 
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
          .HandleTransientHttpError()
          .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
          .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
