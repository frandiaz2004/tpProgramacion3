using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;

namespace Tp3prog.DAO;

public class StudentDAO
{
    private readonly AppDbContext context;

    public StudentDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Student> GetAll() => context.Students.ToList();
    public Student? GetById(long id) => context.Students.Find(id);

    public Student Create(Student student)
    {
        context.Students.Add(student);
        context.SaveChanges();
        return student;
    }

    public bool Update(long id, Student student)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        context.Entry(existing).CurrentValues.SetValues(student);
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        var student = GetById(id);
        if (student is null) return false;
        context.Students.Remove(student);
        context.SaveChanges();
        return true;
    }
}