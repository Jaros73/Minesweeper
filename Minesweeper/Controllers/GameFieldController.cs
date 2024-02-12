using System.Diagnostics.Eventing.Reader;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Services;

namespace Minesweeper.Controllers;

/// <summary>
/// Controller pro správu herních polí.
/// </summary>

[ApiController]
[Route("[controller]")]
public class GameFieldController : ControllerBase
{
    private readonly IGameFieldService _service;
    private readonly ILogger<GameFieldController> _logger;

    /// <summary>
    /// Inicializuje novou instanci třídy <see cref="GameFieldController"/>.
    /// </summary>
    /// <param name="service">Služba herních polí.</param>
    /// <param name="logger">Logger pro zaznamenávání logů.</param>
    public GameFieldController(IGameFieldService service, ILogger<GameFieldController> logger)
    { _service = service; _logger = logger; }

    /// <summary>
    /// Získá herní pole podle ID hry.
    /// </summary>
    /// <param name="gameId">ID hry.</param>
    /// <returns>ActionResult s IEnumerable GameFieldDto.</returns>
    [HttpGet("{gameId}")]
    public async Task<ActionResult<IEnumerable<GameFieldDto>>> GetGameFields(int gameId)
    {
        try { 
        var gameFields = await _service.GetGameFields(gameId);
        return Ok(gameFields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při získávání herního pole s ID {gameId}.", gameId);
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při zpracování.");
        }
    }

    /// <summary>
    /// Odkryje herní pole.
    /// </summary>
    /// <param name="gameId">ID hry.</param>
    /// <param name="fieldId">ID pole k odkrytí.</param>
    /// <returns>ActionResult s GameFieldDto.</returns>
    [HttpPost("reveal/{gameId}/{fieldId}")]
    public async Task<ActionResult<GameFieldDto>> RevealField(int gameId, int fieldId)
    {
        try
        {
            var gameField = await _service.RevealField(gameId, fieldId);
            return Ok(gameField);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Chyba při odkrývání herního pole s ID game {GameId} a ID field {FieldId}.", gameId, fieldId);

            // BadRequest (400).
            return BadRequest("Požadavek nelze zpracovat kvůli neplatné operaci.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při získávání herního pole s ID {gameId}.", gameId);
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při zpracování.");
        }
    }
}
