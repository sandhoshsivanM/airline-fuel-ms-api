using AirlineFuelMS.Core.DTOs.Airline;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AirlinesController : ControllerBase
{
    private readonly IAirlineService _service;
    public AirlinesController(IAirlineService service) => _service = service;

    /// <summary>
    /// Paginated, sortable, filterable list.
    /// Query params: page, pageSize, sortBy, sortDirection, search, country, isActive.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AirlineQuery query) =>
        Ok(await _service.GetAllAsync(query));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await _service.GetSummaryAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] AirlineCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] AirlineUpdateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
