using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class TeamsController : ControllerBase
{
    private readonly TeamDAO teamDAO;

    public TeamsController(TeamDAO teamDAO)
    {
        this.teamDAO = teamDAO;
    }

    [HttpGet]
    public ActionResult<List<TeamResponse>> GetAll() => Ok(teamDAO.GetAll().Select(team => team.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<TeamResponse> GetById(long id) => teamDAO.GetById(id) is { } team ? Ok(team.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<TeamResponse> Create(TeamRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear el equipo; omitilo o enviá 0.");
        var created = teamDAO.Create(request.ToEntity());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, TeamRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return teamDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => teamDAO.Delete(id) ? NoContent() : NotFound();

    [HttpPut("{id:long}/players/{playerId:long}")]
    public IActionResult AddPlayer(long id, long playerId) => RelationshipResponse(teamDAO.AddPlayer(id, playerId));

    [HttpDelete("{id:long}/players/{playerId:long}")]
    public IActionResult RemovePlayer(long id, long playerId) => RelationshipResponse(teamDAO.RemovePlayer(id, playerId));

    [HttpPut("{id:long}/trainers/{trainerId:long}")]
    public IActionResult AddTrainer(long id, long trainerId) => RelationshipResponse(teamDAO.AddTrainer(id, trainerId));

    [HttpDelete("{id:long}/trainers/{trainerId:long}")]
    public IActionResult RemoveTrainer(long id, long trainerId) => RelationshipResponse(teamDAO.RemoveTrainer(id, trainerId));

    private IActionResult RelationshipResponse(RelationshipResult result) => result switch
    {
        RelationshipResult.Success => NoContent(),
        RelationshipResult.NotFound => NotFound("El equipo o el registro relacionado no existe."),
        _ => Conflict("No se pudo guardar la asociación.")
    };
}
