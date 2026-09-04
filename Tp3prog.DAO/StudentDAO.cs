using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class StudentDAO
{
    public List<Student> GetAll() => MockDatabase.Students;
    public Student? GetById(long id) => MockDatabase.Students.FirstOrDefault(student => student.Id == id);

    public Student Create(Student student)
    {
        student.Id = MockDatabase.Students.Count == 0 ? 1 : MockDatabase.Students.Max(item => item.Id) + 1;
        MockDatabase.Students.Add(student);
        return student;
    }

    public bool Update(long id, Student student)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        student.Id = id;
        MockDatabase.Students[MockDatabase.Students.IndexOf(existing)] = student;
        return true;
    }

    public bool Delete(long id)
    {
        var student = GetById(id);
        return student is not null && MockDatabase.Students.Remove(student);
    }
}