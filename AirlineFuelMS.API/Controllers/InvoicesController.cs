using AirlineFuelMS.Core.DTOs.Invoice;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    public InvoicesController(IInvoiceService service) => _service = service;

    /// <summary>
    /// Paginated list. Status auto-flips to "Overdue" when DueDate is in the past
    /// (the stored Status only changes if an admin explicitly sets it).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceQuery query) =>
        Ok(await _service.GetAllAsync(query));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Explicitly create an invoice for a transaction that doesn't have one yet.
    /// Returns 409 if the transaction already has an invoice.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] InvoiceCreateDto dto)
    {
        try
        {
            var result = await _service.CreateExplicitAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] InvoiceUpdateStatusDto dto)
    {
        var result = await _service.UpdateStatusAsync(id, dto.Status);
        return result is null ? NotFound() : Ok(result);
    }
}
