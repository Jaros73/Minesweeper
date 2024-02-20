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

    /// <summary>
    /// Zpracuje kliknutí uživatele na specifické herní pole v rámci dané hry.
    /// Tato metoda nejprve ověří, zda se ID hry v URL shoduje s ID hry v těle požadavku.
    /// Poté načte hru a ověří její existenci a aktivní stav. Nalezne specifické herní pole
    /// na základě souřadnic X a Y poskytnutých v těle požadavku. Pokud pole již bylo odhaleno
    /// nebo obsahuje minu, vrátí informace o tomto poli. Pokud pole nebylo odhaleno, metoda
    /// ho odhalí, aktualizuje stav hry v případě, že pole obsahuje minu, a pokud ne, pokusí se
    /// rekurzivně odhalit sousední pole bez min. Nakonec uloží změny do databáze a vrátí
    /// aktualizované informace o kliknutém herním poli.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se má pole odhalit. Tento parametr musí být součástí URL cesty.</param>
    /// <param name="input">Objekt obsahující souřadnice X a Y herního pole, na které bylo kliknuto, spolu s ID hry pro další ověření. Tento objekt je přijat v těle požadavku.</param>


    [HttpPost("{gameId}/click")]
    public async Task<ActionResult<GameFieldDto>> Click([FromRoute] int gameId, [FromBody] GameClickDto input)
    {
        _logger.LogInformation($"Zpracovává se kliknutí pro hru {gameId} s vstupem X: {input.X}, Y: {input.Y}.");

        if (gameId != input.GameId)
        {
            _logger.LogWarning($"ID hry v URL ({gameId}) a těle požadavku ({input.GameId}) se neshodují.");
            return BadRequest("ID hry v URL a těle požadavku se neshodují.");
        }

        try
        {
            var gameFieldDto = await _service.Click(gameId, input);
            if (gameFieldDto == null)
            {
                _logger.LogWarning($"Specifikované herní pole nebylo nalezeno pro hru {gameId}.");
                return NotFound("Specifikované herní pole nebylo nalezeno.");
            }

            _logger.LogInformation($"Kliknutí na pole pro hru {gameId} bylo úspěšně zpracováno.");
            return Ok(gameFieldDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, $"Operace Click selhala pro hru ID {gameId}.");
            return BadRequest("Nelze zpracovat požadavek kvůli neplatné operaci.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Chyba při zpracování kliknutí na herní pole pro hru ID {gameId}.");
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při zpracování kliknutí.");
        }
    }

}
