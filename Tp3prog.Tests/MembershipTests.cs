using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tp3prog.Contracts;
using Tp3prog.Controllers;
using Tp3prog.DAO;
using Tp3prog.DAO.EntityFramework;
using Tp3prog.Entities;
using Xunit;

namespace Tp3prog.Tests;

public class MembershipTests
{
    [Theory]
    [InlineData("Teams", "{\"name\":\"Equipo\",\"category\":\"Primera\"}")]
    [InlineData("Teams", "{\"name\":\"Equipo\",\"category\":\"Primera\",\"playerIds\":[]}")]
    [InlineData("Teams", "{\"name\":\"Equipo\",\"category\":\"Primera\",\"playerIds\":null}")]
    [InlineData("Teams", "{\"name\":\"Equipo\",\"category\":\"Primera\",\"playerIds\":[0]}")]
    [InlineData("Courses", "{\"name\":\"Curso\"}")]
    [InlineData("Courses", "{\"name\":\"Curso\",\"studentIds\":[]}")]
    [InlineData("Courses", "{\"name\":\"Curso\",\"studentIds\":null}")]
    [InlineData("Courses", "{\"name\":\"Curso\",\"studentIds\":[-1]}")]
    public async Task Create_rejects_missing_empty_or_invalid_member_ids(string resource, string json)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/" + resource,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var list = await client.GetAsync("/" + resource);
        Assert.Equal("[]", await list.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_links_existing_members_and_collapses_duplicate_ids(bool team)
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var member = AddMember(context, team);
        if (team)
        {
            var controller = new TeamsController(new TeamDAO(context));
            var result = controller.Create(new CreateTeamRequest
                { Name = "Equipo", Category = "Primera", PlayerIds = [member, member] });
            var response = Assert.IsType<TeamResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            Assert.Equal(member, Assert.Single(response.Players).Id);
        }
        else
        {
            var controller = new CoursesController(new CourseDAO(context));
            var result = controller.Create(new CreateCourseRequest
                { Name = "Curso", StudentIds = [member, member] });
            var response = Assert.IsType<CourseResponse>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            Assert.Equal(member, Assert.Single(response.Students).Id);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_with_one_missing_member_does_not_save_a_partial_parent(bool team)
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var member = AddMember(context, team);
        IActionResult? result = team
            ? new TeamsController(new TeamDAO(context)).Create(new CreateTeamRequest
                { Name = "Equipo", Category = "Primera", PlayerIds = [member, long.MaxValue] }).Result
            : new CoursesController(new CourseDAO(context)).Create(new CreateCourseRequest
                { Name = "Curso", StudentIds = [member, long.MaxValue] }).Result;
        Assert.IsType<NotFoundObjectResult>(result);
        using var verify = database.CreateContext();
        Assert.Empty(verify.Teams);
        Assert.Empty(verify.Courses);
        Assert.Single(verify.Persons);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Dao_also_rejects_creating_an_empty_parent(bool team)
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        var result = team
            ? new TeamDAO(context).Create(new Team { Name = "Equipo", Category = "Primera" }, [])
            : new CourseDAO(context).Create(new Course { Name = "Curso" }, []);
        Assert.Equal(RelationshipResult.MinimumRequired, result);
        Assert.Empty(context.Teams);
        Assert.Empty(context.Courses);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Removing_the_last_member_returns_conflict_and_preserves_the_link(bool team)
    {
        using var database = new TestDatabase();
        var (parent, first, _) = Seed(database, team, false);
        using var context = database.CreateContext();
        var result = team
            ? new TeamsController(new TeamDAO(context)).RemovePlayer(parent, first)
            : new CoursesController(new CourseDAO(context)).RemoveStudent(parent, first);
        Assert.IsType<ConflictObjectResult>(result);
        using var verify = database.CreateContext();
        Assert.Equal(1, CountMembers(verify, team, parent));
        Assert.NotNull(verify.Persons.Find(first));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Removal_is_idempotent_and_rechecks_counts_after_a_previous_read(bool team)
    {
        using var database = new TestDatabase();
        var (parent, first, second) = Seed(database, team, true);
        using var stale = database.CreateContext();
        if (team) new TeamDAO(stale).GetById(parent);
        else new CourseDAO(stale).GetById(parent);
        using (var writer = database.CreateContext())
        {
            Assert.Equal(RelationshipResult.Success, Remove(writer, team, parent, first));
            Assert.Equal(RelationshipResult.Success, Remove(writer, team, parent, first));
        }
        Assert.Equal(RelationshipResult.MinimumRequired, Remove(stale, team, parent, second));
        using var verify = database.CreateContext();
        Assert.Equal(1, CountMembers(verify, team, parent));
        Assert.Equal(2, verify.Persons.Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deleting_a_member_is_atomic_when_any_parent_would_become_empty(bool team)
    {
        using var database = new TestDatabase();
        var (parent, member, _) = Seed(database, team, true);
        long otherParent;
        using (var setup = database.CreateContext())
        {
            if (team)
            {
                var other = new Team { Name = "Otro equipo", Category = "Primera", Players = [setup.Players.Find(member)!] };
                setup.Add(other); setup.SaveChanges(); otherParent = other.Id;
            }
            else
            {
                var other = new Course { Name = "Otro curso", Students = [setup.Students.Find(member)!] };
                setup.Add(other); setup.SaveChanges(); otherParent = other.Id;
            }
        }
        using (var context = database.CreateContext())
        {
            var result = team
                ? new PlayersController(new PlayerDAO(context)).Delete(member)
                : new StudentsController(new StudentDAO(context)).Delete(member);
            Assert.IsType<ConflictObjectResult>(result);
        }
        using var verify = database.CreateContext();
        Assert.NotNull(verify.Persons.Find(member));
        Assert.Equal(2, CountMembers(verify, team, parent));
        Assert.Equal(1, CountMembers(verify, team, otherParent));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deleting_a_member_with_a_replacement_preserves_the_remaining_member(bool team)
    {
        using var database = new TestDatabase();
        var (parent, member, remaining) = Seed(database, team, true);
        using (var context = database.CreateContext())
        {
            Assert.Equal(RelationshipResult.Success, team
                ? new PlayerDAO(context).Delete(member) : new StudentDAO(context).Delete(member));
        }
        using var verify = database.CreateContext();
        Assert.Null(verify.Persons.Find(member));
        Assert.NotNull(verify.Persons.Find(remaining));
        Assert.Equal(1, CountMembers(verify, team, parent));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scalar_updates_preserve_memberships_and_parent_deletion_preserves_people(bool team)
    {
        using var database = new TestDatabase();
        var (parent, member, _) = Seed(database, team, false);
        using (var context = database.CreateContext())
        {
            var result = team
                ? new TeamsController(new TeamDAO(context)).Update(parent, new TeamRequest { Name = "Editado", Category = "Primera" })
                : new CoursesController(new CourseDAO(context)).Update(parent, new CourseRequest { Name = "Editado" });
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(1, CountMembers(context, team, parent));
            Assert.True(team ? new TeamDAO(context).Delete(parent) : new CourseDAO(context).Delete(parent));
        }
        using var verify = database.CreateContext();
        Assert.NotNull(verify.Persons.Find(member));
    }

    private static long AddMember(AppDbContext context, bool team)
    {
        Person member = team ? new Player { Name = "Jugador", Age = 20, Number = 10 }
            : new Student { Name = "Alumno", Age = 20, File = "A-01" };
        context.Add(member); context.SaveChanges(); return member.Id;
    }

    private static (long Parent, long First, long Second) Seed(TestDatabase database, bool team, bool two)
    {
        using var context = database.CreateContext();
        var first = AddMember(context, team);
        var second = two ? AddMember(context, team) : 0;
        if (team)
        {
            var parent = new Team { Name = "Equipo", Category = "Primera", Players = [context.Players.Find(first)!] };
            if (two) parent.Players.Add(context.Players.Find(second)!);
            context.Add(parent); context.SaveChanges(); return (parent.Id, first, second);
        }
        else
        {
            var parent = new Course { Name = "Curso", Students = [context.Students.Find(first)!] };
            if (two) parent.Students.Add(context.Students.Find(second)!);
            context.Add(parent); context.SaveChanges(); return (parent.Id, first, second);
        }
    }

    private static int CountMembers(AppDbContext context, bool team, long id) => team
        ? context.Teams.Where(x => x.Id == id).Select(x => x.Players.Count).Single()
        : context.Courses.Where(x => x.Id == id).Select(x => x.Students.Count).Single();

    private static RelationshipResult Remove(AppDbContext context, bool team, long parent, long member) => team
        ? new TeamDAO(context).RemovePlayer(parent, member) : new CourseDAO(context).RemoveStudent(parent, member);
}
