FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Hub.Identity.API.csproj", "./"]
RUN dotnet restore "./Hub.Identity.API.csproj"
COPY . .
RUN dotnet build "Hub.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hub.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hub.Identity.API.dll"]