namespace CarDatabase.Models;

public class Owner
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    // Navigation property - коллекция машин этого владельца
    public ICollection<Car> Cars { get; set; } = new List<Car>();
}