using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tp3prog.Contracts;
using Tp3prog.Controllers;
using Tp3prog.DAO;
using Tp3prog.Entities;
using Xunit;

namespace Tp3prog.Tests;

public class RelationshipTests
{
    [Fact]
    public void Enrollment_is_idempotent_loads_from_both_sides_and_removal_preserves_student()
    {
        using var database = new TestDatabase();
        long courseId, studentId;
        using (var setup = database.CreateContext())
        {
            var course = new Course { Name = "Programación",
                Students = [new Student { Name = "Alumno inicial", Age = 20, File = "BASE" }] };
            var student = new Student { Name = "Estudiante", Age = 20, File = "123" };
            setup.AddRange(course, student);
            setup.SaveChanges();
            courseId = course.Id;
            studentId = student.Id;
        }

        using (var context = database.CreateContext())
        {
            var dao = new CourseDAO(context);
            Assert.Equal(RelationshipResult.Success, dao.AddStudent(courseId, studentId));
            Assert.Equal(RelationshipResult.Success, dao.AddStudent(courseId, studentId));
        }

        using (var verify = database.CreateContext())
        {
            Assert.Contains(new CourseDAO(verify).GetById(courseId)!.Students, student => student.Id == studentId);
            Assert.Equal(courseId, Assert.Single(new StudentDAO(verify).GetById(studentId)!.Courses).Id);
            Assert.Equal(2, new CourseDAO(verify).GetAll().Single().Students.Count);
            Assert.All(new StudentDAO(verify).GetAll(), student => Assert.Single(student.Courses));

            var controller = new CoursesController(new CourseDAO(verify));
            var ok = Assert.IsType<OkObjectResult>(controller.GetById(courseId).Result);
            var response = Assert.IsType<CourseResponse>(ok.Value);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(response));
            Assert.Contains(json.RootElement.GetProperty("Students").EnumerateArray(),
                student => student.GetProperty("Id").GetInt64() == studentId);
            Assert.False(json.RootElement.GetProperty("Students")[0].TryGetProperty("Courses", out _));
        }

        using (var context = database.CreateContext())
        {
            var dao = new CourseDAO(context);
            Assert.Equal(RelationshipResult.Success, dao.RemoveStudent(courseId, studentId));
            Assert.Equal(RelationshipResult.Success, dao.RemoveStudent(courseId, studentId));
        }

        using var final = database.CreateContext();
        Assert.Single(new CourseDAO(final).GetById(courseId)!.Students);
        Assert.NotNull(final.Students.SingleOrDefault(x => x.Id == studentId));
    }

    [Fact]
    public void Missing_student_returns_404_without_mutating_the_course()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var course = new Course { Name = "Curso" };
        context.Add(course);
        context.SaveChanges();
        var controller = new CoursesController(new CourseDAO(context));
        Assert.IsType<NotFoundObjectResult>(controller.AddStudent(course.Id, 999));
        Assert.IsType<NotFoundObjectResult>(controller.AddStudent(999, 999));
        Assert.DoesNotContain(context.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified);
    }

    [Fact]
    public void Activity_cannot_move_between_courses_without_explicit_removal()
    {
        using var database = new TestDatabase();
        long firstId, secondId, activityId;
        using (var setup = database.CreateContext())
        {
            var first = new Course { Name = "Primero" };
            var second = new Course { Name = "Segundo" };
            var activity = new Activity { Title = "Examen", Description = "Descripción" };
            setup.AddRange(first, second, activity);
            setup.SaveChanges();
            firstId = first.Id; secondId = second.Id; activityId = activity.Id;
        }

        using (var context = database.CreateContext())
        {
            var dao = new CourseDAO(context);
            Assert.Equal(RelationshipResult.Success, dao.AddActivity(firstId, activityId));
            Assert.Equal(RelationshipResult.Success, dao.AddActivity(firstId, activityId));
        }

        using (var context = database.CreateContext())
        {
            var controller = new CoursesController(new CourseDAO(context));
            Assert.IsType<ConflictObjectResult>(controller.AddActivity(secondId, activityId));
        }

        using (var context = database.CreateContext())
        {
            var dao = new CourseDAO(context);
            Assert.Single(dao.GetById(firstId)!.Activities);
            Assert.Empty(dao.GetById(secondId)!.Activities);
            Assert.Equal(RelationshipResult.Success, dao.RemoveActivity(firstId, activityId));
            Assert.Equal(RelationshipResult.Success, dao.RemoveActivity(firstId, activityId));
            Assert.NotNull(context.Activities.SingleOrDefault(x => x.Id == activityId));
            Assert.Equal(RelationshipResult.Success, dao.AddActivity(secondId, activityId));
        }

        using var verify = database.CreateContext();
        Assert.Empty(new CourseDAO(verify).GetById(firstId)!.Activities);
        Assert.Single(new CourseDAO(verify).GetById(secondId)!.Activities);
    }

    [Fact]
    public void Team_memberships_load_both_sides_and_unlinking_preserves_people()
    {
        using var database = new TestDatabase();
        long teamId, playerId, trainerId;
        using (var setup = database.CreateContext())
        {
            var team = new Team { Name = "Equipo", Category = "Adultos",
                Players = [new Player { Name = "Jugador inicial", Age = 25, Number = 1 }] };
            var player = new Player { Name = "Jugador", Age = 25, Number = 10 };
            var trainer = new Trainer { Name = "Entrenador", Age = 35 };
            setup.AddRange(team, player, trainer);
            setup.SaveChanges();
            teamId = team.Id; playerId = player.Id; trainerId = trainer.Id;
        }

        using (var context = database.CreateContext())
        {
            var dao = new TeamDAO(context);
            Assert.Equal(RelationshipResult.Success, dao.AddPlayer(teamId, playerId));
            Assert.Equal(RelationshipResult.Success, dao.AddPlayer(teamId, playerId));
            Assert.Equal(RelationshipResult.Success, dao.AddTrainer(teamId, trainerId));
            Assert.Equal(RelationshipResult.Success, dao.AddTrainer(teamId, trainerId));
            Assert.Equal(RelationshipResult.NotFound, dao.AddPlayer(teamId, 999));
            Assert.Equal(RelationshipResult.NotFound, dao.AddTrainer(999, trainerId));
        }

        using (var verify = database.CreateContext())
        {
            var team = new TeamDAO(verify).GetById(teamId)!;
            Assert.Equal(2, team.Players.Count);
            Assert.Single(team.Trainers);
            Assert.Single(new PlayerDAO(verify).GetById(playerId)!.Teams);
            Assert.Single(new TrainerDAO(verify).GetById(trainerId)!.Teams);
            Assert.Equal(2, new TeamDAO(verify).GetAll().Single().Players.Count);
            Assert.All(new PlayerDAO(verify).GetAll(), player => Assert.Single(player.Teams));
            Assert.Single(new TrainerDAO(verify).GetAll().Single().Teams);
            var controller = new TeamsController(new TeamDAO(verify));
            var response = Assert.IsType<OkObjectResult>(controller.GetById(teamId).Result).Value;
            Assert.NotEmpty(JsonSerializer.Serialize(response));
        }

        using (var context = database.CreateContext())
        {
            var dao = new TeamDAO(context);
            Assert.Equal(RelationshipResult.Success, dao.RemovePlayer(teamId, playerId));
            Assert.Equal(RelationshipResult.Success, dao.RemovePlayer(teamId, playerId));
            Assert.Equal(RelationshipResult.Success, dao.RemoveTrainer(teamId, trainerId));
            Assert.Equal(RelationshipResult.Success, dao.RemoveTrainer(teamId, trainerId));
        }

        using var final = database.CreateContext();
        var finalTeam = new TeamDAO(final).GetById(teamId)!;
        Assert.Single(finalTeam.Players);
        Assert.Empty(finalTeam.Trainers);
        Assert.NotNull(final.Players.SingleOrDefault(x => x.Id == playerId));
        Assert.NotNull(final.Trainers.SingleOrDefault(x => x.Id == trainerId));
    }

    [Fact]
    public void Deleting_a_course_keeps_its_activities_and_students()
    {
        using var database = new TestDatabase();
        long courseId;
        using (var setup = database.CreateContext())
        {
            var course = new Course
            {
                Name = "Curso",
                Students = [new Student { Name = "Estudiante", Age = 20, File = "123" }],
                Activities = [new Activity { Title = "Examen", Description = "Descripción" }]
            };
            setup.Add(course);
            setup.SaveChanges();
            courseId = course.Id;
        }

        using (var context = database.CreateContext())
            Assert.True(new CourseDAO(context).Delete(courseId));

        using var verify = database.CreateContext();
        Assert.Empty(verify.Courses);
        Assert.Single(verify.Students);
        Assert.Single(verify.Activities);
        Assert.Null(verify.Entry(verify.Activities.Single()).Property<long?>("CourseId").CurrentValue);
    }
}
