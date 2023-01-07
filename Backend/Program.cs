using Bogus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var _testCustomers = new Faker<Customer>()
                            .RuleFor(x => x.Name, f => f.Name.FullName())
                            .RuleFor(x => x.City, f => f.Address.City())
                            .RuleFor(x => x.Id, f => f.Database.Random.Guid()
                            );

app.MapGet("/Customer", () =>
{
    var faker = new Faker("en");
    return Enumerable.Range(1, 1000000).Select(index => _testCustomers.Generate()).Select(x => new Page()
    {
        Customer = x,
        Minute = faker.Random.Number(int.MaxValue),
    })
    .ToArray();
})
.WithName("GetCustomer")
.WithOpenApi();

app.Run();



