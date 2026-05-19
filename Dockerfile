FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Perspektif.API/Perspektif.API.csproj", "Perspektif.API/"]
RUN dotnet restore "Perspektif.API/Perspektif.API.csproj"
COPY . .
WORKDIR "/src/Perspektif.API"
RUN dotnet publish "Perspektif.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "Perspektif.API.dll"]
