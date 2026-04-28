FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FlightReservationAdminWeb/FlightReservationAdminWeb.csproj", "FlightReservationAdminWeb/"]
RUN dotnet restore "FlightReservationAdminWeb/FlightReservationAdminWeb.csproj"

COPY . .
WORKDIR "/src/FlightReservationAdminWeb"
RUN dotnet publish "FlightReservationAdminWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5002
EXPOSE 5003

ENTRYPOINT ["dotnet", "FlightReservationAdminWeb.dll"]
