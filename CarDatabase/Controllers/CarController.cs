using CarDatabase.Data;
using CarDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarDatabase.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]   // ⬅️ ДЛЯ ЛАБЫ ВРЕМЕННО ОТКЛЮЧАЕМ
public class CarController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CarController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/car
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetCars()
    {
        var cars = await _context.Cars
            .Include(c => c.Owner)
            .Select(c => new
            {
                c.Id,
                c.Brand,
                c.Model,
                c.Color,
                c.Year,
                c.Price,
                Owner = new
                {
                    c.Owner.Id,
                    c.Owner.FirstName,
                    c.Owner.LastName
                }
            })
            .ToListAsync();

        return Ok(cars);
    }

    // GET: api/car/5
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetCar(int id)
    {
        var car = await _context.Cars
            .Include(c => c.Owner)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Brand,
                c.Model,
                c.Color,
                c.Year,
                c.Price,
                Owner = new
                {
                    c.Owner.Id,
                    c.Owner.FirstName,
                    c.Owner.LastName
                }
            })
            .FirstOrDefaultAsync();

        if (car == null)
        {
            return NotFound(new { message = $"Car with id {id} not found" });
        }

        return Ok(car);
    }

    // POST: api/car
    [HttpPost]
    public async Task<ActionResult<Car>> CreateCar(CarDto carDto)
    {
        // Проверяем существует ли владелец
        var ownerExists = await _context.Owners.AnyAsync(o => o.Id == carDto.OwnerId);
        if (!ownerExists)
        {
            return BadRequest(new { message = $"Owner with id {carDto.OwnerId} not found" });
        }

        var car = new Car
        {
            Brand = carDto.Brand,
            Model = carDto.Model,
            Color = carDto.Color,
            Year = carDto.Year,
            Price = carDto.Price,
            OwnerId = carDto.OwnerId
        };

        _context.Cars.Add(car);
        await _context.SaveChangesAsync();

        // Загружаем владельца для ответа
        await _context.Entry(car).Reference(c => c.Owner).LoadAsync();

        return CreatedAtAction(nameof(GetCar), new { id = car.Id }, new
        {
            car.Id,
            car.Brand,
            car.Model,
            car.Color,
            car.Year,
            car.Price,
            Owner = new
            {
                car.Owner.Id,
                car.Owner.FirstName,
                car.Owner.LastName
            }
        });
    }

    // PUT: api/car/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCar(int id, CarDto carDto)
    {
        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return NotFound(new { message = $"Car with id {id} not found" });
        }

        // Проверяем существует ли владелец
        var ownerExists = await _context.Owners.AnyAsync(o => o.Id == carDto.OwnerId);
        if (!ownerExists)
        {
            return BadRequest(new { message = $"Owner with id {carDto.OwnerId} not found" });
        }

        car.Brand = carDto.Brand;
        car.Model = carDto.Model;
        car.Color = carDto.Color;
        car.Year = carDto.Year;
        car.Price = carDto.Price;
        car.OwnerId = carDto.OwnerId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/car/5
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchCar(int id, CarPatchDto patchDto)
    {
        var car = await _context.Cars.FindAsync(id);

        if (car == null)
        {
            return NotFound(new { message = $"Car with id {id} not found" });
        }

        // Обновляем только те поля, которые переданы
        if (!string.IsNullOrEmpty(patchDto.Brand))
            car.Brand = patchDto.Brand;
        
        if (!string.IsNullOrEmpty(patchDto.Model))
            car.Model = patchDto.Model;
        
        if (!string.IsNullOrEmpty(patchDto.Color))
            car.Color = patchDto.Color;
        
        if (patchDto.Year.HasValue)
            car.Year = patchDto.Year.Value;
        
        if (patchDto.Price.HasValue)
            car.Price = patchDto.Price.Value;

        if (patchDto.OwnerId.HasValue)
        {
            var ownerExists = await _context.Owners.AnyAsync(o => o.Id == patchDto.OwnerId.Value);
            if (!ownerExists)
            {
                return BadRequest(new { message = $"Owner with id {patchDto.OwnerId.Value} not found" });
            }
            car.OwnerId = patchDto.OwnerId.Value;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/car/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        
        if (car == null)
        {
            return NotFound(new { message = $"Car with id {id} not found" });
        }

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTO для создания/обновления машины (полное обновление)
public class CarDto
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int OwnerId { get; set; }
}

// DTO для частичного обновления (PATCH)
public class CarPatchDto
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public int? Year { get; set; }
    public decimal? Price { get; set; }
    public int? OwnerId { get; set; }
}
