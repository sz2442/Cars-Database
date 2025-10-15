namespace CarDatabase.Models;

public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    
    // Foreign Key - ID владельца
    public int OwnerId { get; set; }
    
    // Navigation property - ссылка на владельца
    public Owner Owner { get; set; } = null!;
}