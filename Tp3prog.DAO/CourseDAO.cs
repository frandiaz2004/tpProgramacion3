using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class CourseDAO
{
    public List<Course> GetAll() => MockDatabase.Courses;
    public Course? GetById(long id) => MockDatabase.Courses.FirstOrDefault(course => course.Id == id);

    public Course Create(Course course)
    {
        course.Id = MockDatabase.Courses.Count == 0 ? 1 : MockDatabase.Courses.Max(item => item.Id) + 1;
        MockDatabase.Courses.Add(course);
        return course;
    }

    public bool Update(long id, Course course)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        course.Id = id;
        MockDatabase.Courses[MockDatabase.Courses.IndexOf(existing)] = course;
        return true;
    }

    public bool Delete(long id)
    {
        var course = GetById(id);
        return course is not null && MockDatabase.Courses.Remove(course);
    }
}