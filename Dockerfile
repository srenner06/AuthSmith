FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/AuthSmith.Api/AuthSmith.Api.csproj", "src/AuthSmith.Api/"]
COPY ["src/AuthSmith.Application/AuthSmith.Application.csproj", "src/AuthSmith.Application/"]
COPY ["src/AuthSmith.Domain/AuthSmith.Domain.csproj", "src/AuthSmith.Domain/"]
COPY ["src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj", "src/AuthSmith.Infrastructure/"]
COPY ["src/AuthSmith.Contracts/AuthSmith.Contracts.csproj", "src/AuthSmith.Contracts/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "src/AuthSmith.Api/AuthSmith.Api.csproj"
COPY . .
WORKDIR "/src/src/AuthSmith.Api"
RUN dotnet build "AuthSmith.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthSmith.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthSmith.Api.dll"]

