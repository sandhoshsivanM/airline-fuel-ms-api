using System.Security.Claims;
using AirlineFuelMS.Core.DTOs.FuelTransaction;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FuelTransactionsController : ControllerBase
{
    private readonly IFuelTransactionService _service;
    public FuelTransactionsController(IFuelTransactionService service) => _service = service;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Paginated, sortable, filterable list.
    /// Query params: page, pageSize, sortBy, sortDirection, search,
    /// airlineId, fuelProviderId, status, fromDate, toDate.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FuelTransactionQuery query) =>
        Ok(await _service.GetAllAsync(query));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FuelTransactionCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FuelTransactionUpdateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
