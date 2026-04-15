using FlightReservationService.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightReservationService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FlightNumber).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CityFrom).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CityTo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReservationNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.ReservationNumber).IsUnique();
            entity.Property(e => e.PassengerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PassengerEmail).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Flight).WithMany().HasForeignKey(e => e.FlightId);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var flights = new List<Flight>
        {
            new() { Id = 1, FlightNumber = "LO101", CityFrom = "Warszawa", CityTo = "Kraków", DepartureDate = new DateTime(2026, 5, 1), DepartureTime = "08:00", Price = 199.99m, AvailableSeats = 120 },
            new() { Id = 2, FlightNumber = "LO202", CityFrom = "Kraków", CityTo = "Warszawa", DepartureDate = new DateTime(2026, 5, 1), DepartureTime = "14:30", Price = 189.99m, AvailableSeats = 95 },
            new() { Id = 3, FlightNumber = "LO303", CityFrom = "Warszawa", CityTo = "Gdańsk", DepartureDate = new DateTime(2026, 5, 2), DepartureTime = "10:15", Price = 249.99m, AvailableSeats = 80 },
            new() { Id = 4, FlightNumber = "LO404", CityFrom = "Gdańsk", CityTo = "Wrocław", DepartureDate = new DateTime(2026, 5, 2), DepartureTime = "16:00", Price = 299.99m, AvailableSeats = 60 },
            new() { Id = 5, FlightNumber = "LO505", CityFrom = "Wrocław", CityTo = "Poznań", DepartureDate = new DateTime(2026, 5, 3), DepartureTime = "09:45", Price = 159.99m, AvailableSeats = 110 },
            new() { Id = 6, FlightNumber = "FR610", CityFrom = "Warszawa", CityTo = "Londyn", DepartureDate = new DateTime(2026, 5, 3), DepartureTime = "06:30", Price = 449.99m, AvailableSeats = 180 },
            new() { Id = 7, FlightNumber = "FR711", CityFrom = "Kraków", CityTo = "Berlin", DepartureDate = new DateTime(2026, 5, 4), DepartureTime = "11:00", Price = 349.99m, AvailableSeats = 150 },
            new() { Id = 8, FlightNumber = "LO808", CityFrom = "Poznań", CityTo = "Warszawa", DepartureDate = new DateTime(2026, 5, 4), DepartureTime = "18:30", Price = 179.99m, AvailableSeats = 100 },
            new() { Id = 9, FlightNumber = "FR912", CityFrom = "Warszawa", CityTo = "Paryż", DepartureDate = new DateTime(2026, 5, 5), DepartureTime = "07:00", Price = 529.99m, AvailableSeats = 200 },
            new() { Id = 10, FlightNumber = "LO110", CityFrom = "Gdańsk", CityTo = "Kraków", DepartureDate = new DateTime(2026, 5, 5), DepartureTime = "13:15", Price = 219.99m, AvailableSeats = 75 },
        };

        modelBuilder.Entity<Flight>().HasData(flights);
    }
}
