#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["LE.ApiGateway/LE.ApiGateway.csproj", "LE.ApiGateway/"]
RUN dotnet restore "LE.ApiGateway/LE.ApiGateway.csproj"
COPY ./ ./

WORKDIR "/src/LE.ApiGateway"
RUN dotnet build "LE.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LE.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final

ARG ASPNETCORE_ENVIRONMENT

ENV ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LE.ApiGateway.dll"]