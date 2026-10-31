using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tp3prog.Contracts;
using Tp3prog.Controllers;
using Tp3prog.DAO;
using Tp3prog.Entities;
using Xunit;

namespace Tp3prog.Tests;

public class RequestTests
{
    [Theory]
    [InlineData("players")]
    [InlineData("students")]
    [InlineData("trainers")]
    [InlineData("courses")]
    [InlineData("teams")]
    [InlineData("activities")]
    public void Update_with_a_different_body_id_rejects_without_changing_the_record(string resource)
    {
        using var database = new TestDatabase();
        long id;
        using (var setup = database.CreateContext())
        {
            var player = new Player { Name = "Original", Age = 25, Number = 10 };
            var student = new Student { Name = "Original", Age = 25, File = "123" };
            var trainer = new Trainer { Name = "Original", Age = 25 };
            var course = new Course { Name = "Original" };
            var team = new Team { Name = "Original", Category = "Adultos" };
            var activity = new Activity { Title = "Original", Description = "Original" };
            setup.AddRange(player, student, trainer, course, team, activity);
            setup.SaveChanges();
            id = resource switch
            {
                "players" => player.Id, "students" => student.Id, "trainers" => trainer.Id,
                "courses" => course.Id, "teams" => team.Id, _ => activity.Id
            };
        }

        using (var context = database.CreateContext())
        {
            var otherId = id + 100;
            var result = resource switch
            {
                "players" => new PlayersController(new PlayerDAO(context)).Update(id,
                    new PlayerRequest { Id = otherId, Name = "Modificado", Age = 30, Number = 1 }),
                "students" => new StudentsController(new StudentDAO(context)).Update(id,
                    new StudentRequest { Id = otherId, Name = "Modificado", Age = 30, File = "456" }),
                "trainers" => new TrainersController(new TrainerDAO(context)).Update(id,
                    new TrainerRequest { Id = otherId, Name = "Modificado", Age = 30 }),
                "courses" => new CoursesController(new CourseDAO(context)).Update(id,
                    new CourseRequest { Id = otherId, Name = "Modificado" }),
                "teams" => new TeamsController(new TeamDAO(context)).Update(id,
                    new TeamRequest { Id = otherId, Name = "Modificado", Category = "Juveniles" }),
                _ => new ActivitiesController(new ActivityDAO(context)).Update(id,
                    new ActivityRequest { Id = otherId, Title = "Modificado", Description = "Modificado" })
            };
            Assert.IsType<BadRequestObjectResult>(result);
        }

        using var verify = database.CreateContext();
        var actual = resource switch
        {
            "players" => verify.Players.Single(x => x.Id == id).Name,
            "students" => verify.Students.Single(x => x.Id == id).Name,
            "trainers" => verify.Trainers.Single(x => x.Id == id).Name,
            "courses" => verify.Courses.Single(x => x.Id == id).Name,
            "teams" => verify.Teams.Single(x => x.Id == id).Name,
            _ => verify.Activities.Single(x => x.Id == id).Title
        };
        Assert.Equal("Original", actual);
    }

    [Fact]
    public void Update_without_body_id_uses_the_route_and_preserves_links()
    {
        using var database = new TestDatabase();
        long id;
        using (var setup = database.CreateContext())
        {
            var player = new Player
            {
                Name = "Original", Age = 25, Number = 10,
                Teams = [new Team { Name = "Equipo", Category = "Adultos" }]
            };
            setup.Add(player);
            setup.SaveChanges();
            id = player.Id;
        }

        using (var context = database.CreateContext())
        {
            var controller = new PlayersController(new PlayerDAO(context));
            Assert.IsType<NoContentResult>(controller.Update(id,
                new PlayerRequest { Name = "Actualizado", Age = 30, Number = 7 }));
        }

        using var verify = database.CreateContext();
        var stored = new PlayerDAO(verify).GetById(id)!;
        Assert.Equal(id, stored.Id);
        Assert.Equal("Actualizado", stored.Name);
        Assert.Single(stored.Teams);
    }

    [Fact]
    public void Create_rejects_a_client_supplied_id_without_inserting()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var controller = new CoursesController(new CourseDAO(context));
        var result = controller.Create(new CreateCourseRequest { Id = 12, Name = "Curso", StudentIds = [1] });
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Courses);
    }

    [Theory]
    [InlineData("{\"name\":\"Curso\",\"students\":[{\"id\":1}]}")]
    [InlineData("{\"name\":\"Curso\",\"studentIds\":[1]}")]
    public void Embedded_relationships_are_rejected_instead_of_silently_ignored(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CourseRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public void Undefined_activity_type_fails_validation()
    {
        var request = new ActivityRequest
        {
            Title = "Examen", Description = "Descripción", Type = (TypeActivity)99
        };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
