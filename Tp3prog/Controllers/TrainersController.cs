using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class TrainersController : ControllerBase
{
    private readonly TrainerDAO trainerDAO;

    public TrainersController(TrainerDAO trainerDAO)
    {
        this.trainerDAO = trainerDAO;
    }

    [HttpGet]
    public ActionResult<List<TrainerResponse>> GetAll() => Ok(trainerDAO.GetAll().Select(trainer => trainer.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<TrainerResponse> GetById(long id) => trainerDAO.GetById(id) is { } trainer ? Ok(trainer.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<TrainerResponse> Create(TrainerRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear el entrenador; omitilo o enviá 0.");
        var created = trainerDAO.Create(request.ToEntity());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, TrainerRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return trainerDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => trainerDAO.Delete(id) ? NoContent() : NotFound();
}
