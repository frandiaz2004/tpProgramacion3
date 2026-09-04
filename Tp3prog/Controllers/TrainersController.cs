using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

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
    public ActionResult<List<Trainer>> GetAll() => Ok(trainerDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Trainer> GetById(long id) => trainerDAO.GetById(id) is { } trainer ? Ok(trainer) : NotFound();

    [HttpPost]
    public ActionResult<Trainer> Create(Trainer trainer)
    {
        var created = trainerDAO.Create(trainer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Trainer trainer) => trainerDAO.Update(id, trainer) ? Ok(trainer) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => trainerDAO.Delete(id) ? NoContent() : NotFound();
}