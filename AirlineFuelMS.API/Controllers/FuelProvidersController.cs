using AirlineFuelMS.Core.DTOs.FuelProvider;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FuelProvidersController : ControllerBase
{
    private readonly IFuelProviderService _service;
    public FuelProvidersController(IFuelProviderService service) => _service = service;

    /// <summary>
    /// Paginated list. ?countryId=N filters to providers operating in that country (via address).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FuelProviderQuery query) =>
        Ok(await _service.GetAllAsync(query));

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries() =>
        Ok(await _service.GetCountriesAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] FuelProviderCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] FuelProviderUpdateDto dto)
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

    // ---- Pricing sub-resource ----

    [HttpGet("{id}/prices")]
    public async Task<IActionResult> GetPrices(int id) =>
        Ok(await _service.GetPricesAsync(id));

    [HttpPost("{id}/prices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddPrice(int id, [FromBody] FuelPriceCreateDto dto)
    {
        var result = await _service.AddPriceAsync(id, dto);
        return result is null ? NotFound(new { message = "Provider not found" }) : Ok(result);
    }

    // ---- Address sub-resource (multi-country) ----

    /// <summary>List all addresses (locations) for a provider across countries.</summary>
    [HttpGet("{id}/addresses")]
    public async Task<IActionResult> GetAddresses(int id) =>
        Ok(await _service.GetAddressesAsync(id));

    [HttpPost("{id}/addresses")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddAddress(int id, [FromBody] FuelProviderAddressCreateDto dto)
    {
        try
        {
            var result = await _service.AddAddressAsync(id, dto);
            return result is null
                ? NotFound(new { message = "Provider not found" })
                : CreatedAtAction(nameof(GetAddresses), new { id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}/addresses/{addressId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAddress(int id, int addressId, [FromBody] FuelProviderAddressUpdateDto dto)
    {
        try
        {
            var result = await _service.UpdateAddressAsync(id, addressId, dto);
            return result is null ? NotFound() : Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id}/addresses/{addressId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAddress(int id, int addressId)
    {
        var deleted = await _service.DeleteAddressAsync(id, addressId);
        return deleted ? NoContent() : NotFound();
    }
}
