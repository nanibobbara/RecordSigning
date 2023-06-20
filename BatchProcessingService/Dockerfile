#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Dockerfile for BatchProcessingService
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
EXPOSE 57101

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BatchProcessingService.csproj", "./"]
RUN dotnet restore "./BatchProcessingService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "BatchProcessingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BatchProcessingService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BatchProcessingService.dll"]
