using Microsoft.AspNetCore.Mvc;
using Tp3prog.DAO;
using Tp3prog.Entities;

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
    public ActionResult<List<Student>> GetAll() => Ok(studentDAO.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<Student> GetById(long id) => studentDAO.GetById(id) is { } student ? Ok(student) : NotFound();

    [HttpPost]
    public ActionResult<Student> Create(Student student)
    {
        var created = studentDAO.Create(student);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, Student student) => studentDAO.Update(id, student) ? Ok(student) : NotFound();

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) => studentDAO.Delete(id) ? NoContent() : NotFound();
}