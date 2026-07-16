using Billing.Api.ExceptionHandlers;
using Billing.Api.Mapping;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Application.Services;
using Billing.Application.Validation;
using Billing.Infrastructure.PaymentGateways;
using FluentValidation;
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

    if (File.Exists(path))
    {
        options.IncludeXmlComments(path);
    }
});

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<BillingMappingProfile>();
    cfg.AddProfile<Billing.Application.Mapping.ApplicationMappingProfile>();
});

builder.Services.AddScoped<IPaymentGateway, SwedbankPaymentGatewayMock>();
builder.Services.AddScoped<IPaymentGateway, SebPaymentGatewayMock>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();
builder.Services.AddScoped<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();
builder.Services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
