#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Dockerfile for RecordKeepingService
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
EXPOSE 57103

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RecordKeepingService.csproj", "./"]
RUN dotnet restore "./RecordKeepingService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RecordKeepingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RecordKeepingService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RecordKeepingService.dll"]
