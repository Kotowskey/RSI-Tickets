FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FlightReservationService/FlightReservationService.csproj", "FlightReservationService/"]
RUN dotnet restore "FlightReservationService/FlightReservationService.csproj"

COPY . .
WORKDIR "/src/FlightReservationService"
RUN dotnet publish "FlightReservationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "FlightReservationService.dll"]
