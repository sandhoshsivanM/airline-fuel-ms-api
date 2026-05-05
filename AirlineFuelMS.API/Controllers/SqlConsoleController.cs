using AirlineFuelMS.Core.DTOs.Admin;
using AirlineFuelMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirlineFuelMS.API.Controllers;

[ApiController]
[Route("api/admin/sql")]
[Authorize(Roles = "Admin")]
public class SqlConsoleController : ControllerBase
{
    private readonly ISqlConsoleService _service;
    public SqlConsoleController(ISqlConsoleService service) => _service = service;

    /// <summary>
    /// Run a read-only SQL query (SELECT / WITH only) against the live database.
    /// Admin-only. Capped at 1000 rows / 15-second timeout.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] SqlQueryRequest req, CancellationToken ct)
    {
        var result = await _service.RunReadOnlyAsync(req.Query, ct);
        return Ok(result);
    }

    /// <summary>List tables + columns of the live database (for the console's schema sidebar).</summary>
    [HttpGet("schema")]
    public async Task<IActionResult> Schema(CancellationToken ct) =>
        Ok(await _service.GetSchemaAsync(ct));
}
