FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["API/WhatsAppParser.API.csproj", "API/"]
COPY ["Application/WhatsAppParser.Application.csproj", "Application/"]
COPY ["Domain/WhatsAppParser.Domain.csproj", "Domain/"]
COPY ["Infrastructure/WhatsAppParser.Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "API/WhatsAppParser.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/API"
RUN dotnet build "WhatsAppParser.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WhatsAppParser.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install ef tools and save migration scripts for entrypoint if needed
ENTRYPOINT ["dotnet", "WhatsAppParser.API.dll"]
