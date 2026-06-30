FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Fcs.Bff.slnx", "."]
COPY ["src/Fcs.Bff.WebApi/Fcs.Bff.WebApi.csproj", "src/Fcs.Bff.WebApi/"]
RUN dotnet restore "src/Fcs.Bff.WebApi/Fcs.Bff.WebApi.csproj"

COPY . .
RUN dotnet publish "src/Fcs.Bff.WebApi/Fcs.Bff.WebApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fcs.Bff.WebApi.dll"]
