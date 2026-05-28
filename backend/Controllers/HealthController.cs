using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DevSecOpsApi.DTOs;
using System.Reflection;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> Get()
    {
        var report  = await healthCheckService.CheckHealthAsync();
        var version = Assembly.GetExecutingAssembly()
                              .GetName().Version?.ToString() ?? "1.0.0";

        var response = new
        {
            Status    = report.Status.ToString(),
            Version   = version,
            Timestamp = DateTime.UtcNow,
            Checks    = report.Entries.Select(e => new
            {
                Name        = e.Key,
                Status      = e.Value.Status.ToString(),
                Description = e.Value.Description
            })
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }
}
