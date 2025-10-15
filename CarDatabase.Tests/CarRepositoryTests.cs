using CarDatabase.Data;
using CarDatabase.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarDatabase.Tests;

public class CarRepositoryTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveCar_ShouldAddCarToDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        // Сначала создаём владельца
        var owner = new Owner { FirstName = "John", LastName = "Doe" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Toyota",
            Model = "Corolla",
            Color = "White",
            Year = 2020,
            Price = 25000,
            OwnerId = owner.Id
        };

        // Act
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Assert
        var savedCar = await context.Cars
            .FirstOrDefaultAsync(c => c.Brand == "Toyota" && c.Model == "Corolla");
        
        savedCar.Should().NotBeNull();
        savedCar!.Brand.Should().Be("Toyota");
        savedCar.Model.Should().Be("Corolla");
        savedCar.Color.Should().Be("White");
        savedCar.OwnerId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task GetAllCars_ShouldReturnAllCars()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Jane", LastName = "Smith" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var cars = new List<Car>
        {
            new() { Brand = "Toyota", Model = "Corolla", Color = "White", Year = 2020, Price = 25000, OwnerId = owner.Id },
            new() { Brand = "Honda", Model = "Civic", Color = "Black", Year = 2021, Price = 28000, OwnerId = owner.Id },
            new() { Brand = "Ford", Model = "Focus", Color = "Blue", Year = 2019, Price = 22000, OwnerId = owner.Id }
        };
        
        context.Cars.AddRange(cars);
        await context.SaveChangesAsync();

        // Act
        var allCars = await context.Cars.ToListAsync();

        // Assert
        allCars.Should().HaveCount(3);
        allCars.Should().Contain(c => c.Brand == "Toyota");
        allCars.Should().Contain(c => c.Brand == "Honda");
        allCars.Should().AllSatisfy(c => c.OwnerId.Should().Be(owner.Id));
    }

    [Fact]
    public async Task DeleteCar_ShouldRemoveCarFromDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Bob", LastName = "Johnson" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Tesla",
            Model = "Model 3",
            Color = "Red",
            Year = 2023,
            Price = 45000,
            OwnerId = owner.Id
        };
        
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        context.Cars.Remove(car);
        await context.SaveChangesAsync();

        // Assert
        var count = await context.Cars.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task FindCarById_ShouldReturnCorrectCar()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Alice", LastName = "Brown" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "BMW",
            Model = "X5",
            Color = "Silver",
            Year = 2022,
            Price = 60000,
            OwnerId = owner.Id
        };
        
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        var foundCar = await context.Cars.FindAsync(car.Id);

        // Assert
        foundCar.Should().NotBeNull();
        foundCar!.Brand.Should().Be("BMW");
        foundCar.Model.Should().Be("X5");
        foundCar.OwnerId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task UpdateCar_ShouldModifyCarInDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Charlie", LastName = "Wilson" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Audi",
            Model = "A4",
            Color = "Black",
            Year = 2021,
            Price = 40000,
            OwnerId = owner.Id
        };
        
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        car.Price = 38000;
        car.Color = "White";
        await context.SaveChangesAsync();

        // Assert
        var updatedCar = await context.Cars.FindAsync(car.Id);
        updatedCar.Should().NotBeNull();
        updatedCar!.Price.Should().Be(38000);
        updatedCar.Color.Should().Be("White");
    }

    [Fact]
    public async Task GetCarWithOwner_ShouldIncludeOwnerData()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "David", LastName = "Lee" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Mercedes",
            Model = "C-Class",
            Color = "Black",
            Year = 2023,
            Price = 55000,
            OwnerId = owner.Id
        };
        
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        var carWithOwner = await context.Cars
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == car.Id);

        // Assert
        carWithOwner.Should().NotBeNull();
        carWithOwner!.Owner.Should().NotBeNull();
        carWithOwner.Owner.FirstName.Should().Be("David");
        carWithOwner.Owner.LastName.Should().Be("Lee");
    }
}