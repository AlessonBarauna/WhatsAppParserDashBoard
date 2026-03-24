FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["API/WhatsAppParser.API.csproj", "API/"]
COPY ["Application/WhatsAppParser.Application.csproj", "Application/"]
COPY ["Domain/WhatsAppParser.Domain.csproj", "Domain/"]
COPY ["Infrastructure/WhatsAppParser.Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "API/WhatsAppParser.API.csproj"

COPY . .
WORKDIR "/src/API"
RUN dotnet publish "WhatsAppParser.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WhatsAppParser.API.dll"]
