using CarDatabase.Data;
using CarDatabase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarDatabase.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")] // ← ТОЛЬКО ДЛЯ АДМИНОВ!
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/admin/users - список всех пользователей
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/admin/stats - статистика
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var stats = new
        {
            TotalCars = await _context.Cars.CountAsync(),
            TotalOwners = await _context.Owners.CountAsync(),
            TotalUsers = await _context.Users.CountAsync(),
            CarsByBrand = await _context.Cars
                .GroupBy(c => c.Brand)
                .Select(g => new { Brand = g.Key, Count = g.Count() })
                .ToListAsync()
        };

        return Ok(stats);
    }

    // DELETE: api/admin/users/{id} - удалить пользователя
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}