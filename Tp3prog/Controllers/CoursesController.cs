using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class CoursesController : ControllerBase
{
    private readonly CourseDAO courseDAO;

    public CoursesController(CourseDAO courseDAO)
    {
        this.courseDAO = courseDAO;
    }

    [HttpGet]
    public ActionResult<List<CourseResponse>> GetAll() => Ok(courseDAO.GetAll().Select(course => course.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<CourseResponse> GetById(long id) => courseDAO.GetById(id) is { } course ? Ok(course.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<CourseResponse> Create(CreateCourseRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear el curso; omitilo o enviá 0.");
        var created = request.ToEntity();
        var result = courseDAO.Create(created, request.StudentIds);
        if (result == RelationshipResult.MinimumRequired) return BadRequest("El curso debe tener al menos un alumno.");
        if (result == RelationshipResult.NotFound) return NotFound("Uno o más alumnos no existen.");
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, CourseRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return courseDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => courseDAO.Delete(id) ? NoContent() : NotFound();

    [HttpPut("{id:long}/students/{studentId:long}")]
    public IActionResult AddStudent(long id, long studentId) => RelationshipResponse(courseDAO.AddStudent(id, studentId));

    [HttpDelete("{id:long}/students/{studentId:long}")]
    public IActionResult RemoveStudent(long id, long studentId) => RelationshipResponse(courseDAO.RemoveStudent(id, studentId));

    [HttpPut("{id:long}/activities/{activityId:long}")]
    public IActionResult AddActivity(long id, long activityId) => RelationshipResponse(courseDAO.AddActivity(id, activityId));

    [HttpDelete("{id:long}/activities/{activityId:long}")]
    public IActionResult RemoveActivity(long id, long activityId) => RelationshipResponse(courseDAO.RemoveActivity(id, activityId));

    private IActionResult RelationshipResponse(RelationshipResult result) => result switch
    {
        RelationshipResult.Success => NoContent(),
        RelationshipResult.NotFound => NotFound("El curso o el registro relacionado no existe."),
        RelationshipResult.MinimumRequired => Conflict("El curso debe conservar al menos un alumno. Agregá otro antes de quitar al último."),
        _ => Conflict("La actividad ya pertenece a otro curso. Quitá esa asociación antes de reasignarla.")
    };
}
