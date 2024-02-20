using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Services;
using Minesweeper.Structures;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Minesweeper.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController : Controller
{
    private readonly IGameService _service;
    private readonly ILogger<GameController> _logger;

    /// <summary>
    /// Inicializuje novou instanci třídy GameController.
    /// </summary>
    /// <param name="service">Poskytuje přístup k operacím souvisejícím s hrami.</param>
    /// <param name="logger">Poskytuje mechanismus pro logování.</param>
    public GameController(IGameService service, ILogger<GameController> logger)
    { _service = service; _logger = logger; }

    /// <summary>
    /// Získání konkrétní hry na základě unikátního klíče.
    /// </summary>
    /// <param name="id">Unikátní klíč hry, která má být získána.</param>
    /// <returns>Akční výsledek obsahující detaily hry, pokud byla nalezena, jinak NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<GameDto>> Get(int id)
    {
        try
        {
            var game = await _service.Get(id);
            if (game == null) return NotFound();

            return Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při získávání hry s ID {Id}.", id);
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při zpracování.");
        }
    }

    /// <summary>
    /// Získání všech aktuálních (rozehraných) her.
    /// </summary>
    /// <returns>Kolekci všech aktuálně rozehraných her.</returns>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<GameDto>>> GetActive()
    {
        try { 
        var games = await _service.GetAllActive();
        if (games == null) return NotFound();
        var gameDtos = games.ToList();
        return Ok(gameDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při získávání aktivních her.");
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při zpracování.");
        }
    }

    /// <summary>
    /// Založení nové hry na základě předaných dat.
    /// </summary>
    /// <param name="input">DTO obsahující data potřebná pro vytvoření nové hry.</param>
    /// <returns>Akční výsledek s HTTP status kódem 201, včetně URI nově vytvořené hry a detailů hry.</returns>
    [HttpPost]
    public async Task<ActionResult<GameDto>> Create([FromBody] GameInputDto input)
    {
        try { 
        var game = await _service.Create(input);
        if (game == null) return BadRequest("Nelze vytvořit hru.");

            _logger.LogInformation("Hra s ID {game.Id} byla úspěšně vytvořena.", game.Id);

            return CreatedAtAction(nameof(Get), new { id = game.Id }, game);
        }
        catch (Exception ex)
        {
            // Logování výjimky
            _logger.LogError(ex, "Chyba při vytváření nové hry");

            // Vrácení obecné chybové odpovědi
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při vytvoření hry.");
        }
    }


    /// <summary>
    /// Smazání hry na základě unikátního klíče.
    /// </summary>
    /// <param name="id">Unikátní klíč hry, která má být smazána.</param>
    /// <returns>Akční výsledek s HTTP status kódem 200, pokud byla hra úspěšně smazána.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.Delete(id);
            _logger.LogInformation("Hra s ID {id} byla smazána.", id);
            return Ok();
        }
        catch (Exception ex)
        {
            // Logování výjimky
            _logger.LogError(ex, "Chyba při smazání nové hry");

            // Vrácení obecné chybové odpovědi
            return StatusCode((int)HttpStatusCode.InternalServerError, "Došlo k chybě při smazání hry.");
        }       
    }
}
