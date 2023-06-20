#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Dockerfile for SigningService
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app
EXPOSE 57102

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SigningService.csproj", "./"]
RUN dotnet restore "./SigningService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SigningService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SigningService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SigningService.dll"]