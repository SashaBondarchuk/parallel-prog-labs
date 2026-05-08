# Dockerfile for Chat Server
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Chat.Server.csproj", "./"]
RUN dotnet restore "Chat.Server.csproj"
COPY . .
RUN dotnet build "Chat.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Chat.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/usr/bin/dotnet", "Chat.Server.dll"]
