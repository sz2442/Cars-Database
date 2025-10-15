using CarDatabase.Data;
using CarDatabase.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarDatabase.Tests;

public class OwnerRepositoryTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveOwner_ShouldAddOwnerToDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var owner = new Owner 
        { 
            FirstName = "Lucy", 
            LastName = "Smith" 
        };

        // Act
        context.Owners.Add(owner);
        await context.SaveChangesAsync();

        // Assert
        var savedOwner = await context.Owners
            .FirstOrDefaultAsync(o => o.FirstName == "Lucy");
        
        savedOwner.Should().NotBeNull();
        savedOwner!.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task DeleteOwner_ShouldRemoveOwnerFromDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var owner = new Owner 
        { 
            FirstName = "Lisa", 
            LastName = "Morrison" 
        };
        
        context.Owners.Add(owner);
        await context.SaveChangesAsync();

        // Act
        context.Owners.Remove(owner);
        await context.SaveChangesAsync();

        // Assert
        var count = await context.Owners.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateOwner_ShouldModifyOwnerInDatabase()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var owner = new Owner 
        { 
            FirstName = "John", 
            LastName = "Doe" 
        };
        
        context.Owners.Add(owner);
        await context.SaveChangesAsync();

        // Act
        owner.LastName = "Smith";
        await context.SaveChangesAsync();

        // Assert
        var updatedOwner = await context.Owners.FindAsync(owner.Id);
        updatedOwner.Should().NotBeNull();
        updatedOwner!.LastName.Should().Be("Smith");
    }
}