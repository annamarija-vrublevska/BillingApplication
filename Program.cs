using Billing.Api.Mapping;
using Billing.Application.Interfaces;
using Billing.Application.Services;
using Billing.Infrastructure.PaymentGateways;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var path = Path.Combine(AppContext.BaseDirectory, xml);

    options.IncludeXmlComments(path);
});

builder.Services.AddProblemDetails();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<BillingMappingProfile>();
    cfg.AddProfile<Billing.Application.Mapping.ApplicationMappingProfile>();
});

builder.Services.AddScoped<IPaymentGateway, SwedbankPaymentGatewayMock>();
builder.Services.AddScoped<IPaymentGateway, SebPaymentGatewayMock>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();
builder.Services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
