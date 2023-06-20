#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Dockerfile for KeyManagementService
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
EXPOSE 57100

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["KeyManagementService.csproj", "./"]
RUN dotnet restore "./KeyManagementService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "KeyManagementService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KeyManagementService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeyManagementService.dll"]

