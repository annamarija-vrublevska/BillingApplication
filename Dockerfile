FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Billing.Api/Billing.Api.csproj Billing.Api/
COPY Billing.Application/Billing.Application.csproj Billing.Application/
COPY Billing.Domain/Billing.Domain.csproj Billing.Domain/
COPY Billing.Infrastructure/Billing.Infrastructure.csproj Billing.Infrastructure/
RUN dotnet restore Billing.Api/Billing.Api.csproj

COPY . .
RUN dotnet publish Billing.Api/Billing.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Billing.Api.dll"]
