using Microsoft.AspNetCore.Mvc;
using BlazorRotatorServer.Models;
using BlazorRotatorServer.Processors;

namespace BlazorRotatorServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RotatorController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            Server.Rotator,
            Server.Serial.Connection
        });
    }
}