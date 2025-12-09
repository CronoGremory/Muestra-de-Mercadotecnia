FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Muestra.csproj", "./"]
RUN dotnet restore "Muestra.csproj"
COPY . .
RUN dotnet build "Muestra.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "Muestra.csproj" -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS="http://+:80"
ENTRYPOINT ["dotnet", "Muestra.dll"]