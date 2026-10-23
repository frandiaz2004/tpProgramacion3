using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Contracts;

namespace Tp3prog.Controllers;

[ApiController]
[Route("[controller]")]
public class StudentsController : ControllerBase
{
    private readonly StudentDAO studentDAO;

    public StudentsController(StudentDAO studentDAO)
    {
        this.studentDAO = studentDAO;
    }

    [HttpGet]
    public ActionResult<List<StudentResponse>> GetAll() => Ok(studentDAO.GetAll().Select(student => student.ToResponse()).ToList());

    [HttpGet("{id:long}")]
    public ActionResult<StudentResponse> GetById(long id) => studentDAO.GetById(id) is { } student ? Ok(student.ToResponse()) : NotFound();

    [HttpPost]
    public ActionResult<StudentResponse> Create(StudentRequest request)
    {
        if (request.Id != 0) return BadRequest("El Id se genera al crear el estudiante; omitilo o enviá 0.");
        var created = studentDAO.Create(request.ToEntity());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, StudentRequest request)
    {
        if (request.Id != 0 && request.Id != id) return BadRequest("El Id del cuerpo debe coincidir con el de la URL.");
        return studentDAO.Update(id, request.ToEntity()) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => studentDAO.Delete(id) ? NoContent() : NotFound();
}
