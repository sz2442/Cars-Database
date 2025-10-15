using CarDatabase.Controllers;
using CarDatabase.Data;
using CarDatabase.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarDatabase.Tests;

public class CarControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetCars_ShouldReturnAllCars()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Test", LastName = "Owner" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        context.Cars.AddRange(
            new Car { Brand = "Toyota", Model = "Corolla", Color = "White", Year = 2020, Price = 25000, OwnerId = owner.Id },
            new Car { Brand = "Honda", Model = "Civic", Color = "Black", Year = 2021, Price = 28000, OwnerId = owner.Id }
        );
        await context.SaveChangesAsync();

        var controller = new CarController(context);

        // Act
        var result = await controller.GetCars();

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var cars = okResult!.Value as IEnumerable<object>;
        cars.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCar_WithValidId_ShouldReturnCar()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Test", LastName = "Owner" };
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
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var controller = new CarController(context);

        // Act
        var result = await controller.GetCar(car.Id);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCar_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var controller = new CarController(context);

        // Act
        var result = await controller.GetCar(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCar_WithValidData_ShouldCreateNewCar()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "New", LastName = "Owner" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var controller = new CarController(context);
        var carDto = new CarDto
        {
            Brand = "Tesla",
            Model = "Model 3",
            Color = "White",
            Year = 2023,
            Price = 45000,
            OwnerId = owner.Id
        };

        // Act
        var result = await controller.CreateCar(carDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        
        // Проверяем что машина действительно добавилась в БД
        var carsInDb = await context.Cars.CountAsync();
        carsInDb.Should().Be(1);
    }

    [Fact]
    public async Task CreateCar_WithInvalidOwnerId_ShouldReturnBadRequest()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var controller = new CarController(context);
        
        var carDto = new CarDto
        {
            Brand = "Tesla",
            Model = "Model 3",
            Color = "White",
            Year = 2023,
            Price = 45000,
            OwnerId = 999 // Несуществующий владелец
        };

        // Act
        var result = await controller.CreateCar(carDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateCar_WithValidData_ShouldUpdateCar()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Update", LastName = "Owner" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Ford", 
            Model = "Focus", 
            Color = "Blue",
            Year = 2019, 
            Price = 22000,
            OwnerId = owner.Id
        };
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var controller = new CarController(context);
        var updateDto = new CarDto
        {
            Brand = "Ford",
            Model = "Focus",
            Color = "Red", // Меняем цвет
            Year = 2019,
            Price = 20000, // Меняем цену
            OwnerId = owner.Id
        };

        // Act
        var result = await controller.UpdateCar(car.Id, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedCar = await context.Cars.FindAsync(car.Id);
        updatedCar!.Price.Should().Be(20000);
        updatedCar.Color.Should().Be("Red");
    }

    [Fact]
    public async Task UpdateCar_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var controller = new CarController(context);
        
        var updateDto = new CarDto
        {
            Brand = "Ford",
            Model = "Focus",
            Color = "Red",
            Year = 2019,
            Price = 20000,
            OwnerId = 1
        };

        // Act
        var result = await controller.UpdateCar(999, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PatchCar_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Patch", LastName = "Owner" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Mazda", 
            Model = "3",
            Color = "Red", 
            Year = 2020, 
            Price = 24000,
            OwnerId = owner.Id
        };
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var controller = new CarController(context);
        var patchDto = new CarPatchDto
        {
            Price = 22000, // Обновляем только цену
            Color = "Blue"  // И цвет
        };

        // Act
        var result = await controller.PatchCar(car.Id, patchDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var updatedCar = await context.Cars.FindAsync(car.Id);
        updatedCar!.Price.Should().Be(22000);
        updatedCar.Color.Should().Be("Blue");
        updatedCar.Brand.Should().Be("Mazda"); // Не изменилось
        updatedCar.Model.Should().Be("3"); // Не изменилось
    }

    [Fact]
    public async Task DeleteCar_WithValidId_ShouldRemoveCar()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        
        var owner = new Owner { FirstName = "Delete", LastName = "Owner" };
        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        
        var car = new Car 
        { 
            Brand = "Nissan", 
            Model = "Altima",
            Color = "Silver", 
            Year = 2020, 
            Price = 26000,
            OwnerId = owner.Id
        };
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var controller = new CarController(context);

        // Act
        var result = await controller.DeleteCar(car.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        var deletedCar = await context.Cars.FindAsync(car.Id);
        deletedCar.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCar_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var controller = new CarController(context);

        // Act
        var result = await controller.DeleteCar(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}