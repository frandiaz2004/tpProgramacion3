using Tp3prog.Entities;

namespace Tp3prog.Contracts;

public static class ContractMappings
{
    public static Player ToEntity(this PlayerRequest request) => new()
    {
        Name = request.Name, Age = request.Age, Dni = request.Dni, Number = request.Number
    };

    public static Student ToEntity(this StudentRequest request) => new()
    {
        Name = request.Name, Age = request.Age, Dni = request.Dni, File = request.File
    };

    public static Trainer ToEntity(this TrainerRequest request) => new()
    {
        Name = request.Name, Age = request.Age, Dni = request.Dni
    };

    public static Team ToEntity(this TeamRequest request) => new() { Name = request.Name, Category = request.Category };
    public static Course ToEntity(this CourseRequest request) => new() { Name = request.Name };
    public static Activity ToEntity(this ActivityRequest request) => new()
    {
        Title = request.Title, Description = request.Description, Date = request.Date, Type = request.Type
    };

    public static PlayerResponse ToResponse(this Player player) => new(player.Id, player.Name, player.Age,
        player.Dni, player.Number, player.Teams.Select(team => new TeamSummary(team.Id, team.Name, team.Category)).ToList());

    public static StudentResponse ToResponse(this Student student) => new(student.Id, student.Name, student.Age,
        student.Dni, student.File, student.Courses.Select(course => new CourseSummary(course.Id, course.Name)).ToList());

    public static TrainerResponse ToResponse(this Trainer trainer) => new(trainer.Id, trainer.Name, trainer.Age,
        trainer.Dni, trainer.Teams.Select(team => new TeamSummary(team.Id, team.Name, team.Category)).ToList());

    public static TeamResponse ToResponse(this Team team) => new(team.Id, team.Name, team.Category,
        team.Players.Select(player => new PlayerSummary(player.Id, player.Name, player.Age, player.Dni, player.Number)).ToList(),
        team.Trainers.Select(trainer => new TrainerSummary(trainer.Id, trainer.Name, trainer.Age, trainer.Dni)).ToList());

    public static CourseResponse ToResponse(this Course course) => new(course.Id, course.Name,
        course.Students.Select(student => new StudentSummary(student.Id, student.Name, student.Age, student.Dni, student.File)).ToList(),
        course.Activities.Select(activity => activity.ToResponse()).ToList());

    public static ActivityResponse ToResponse(this Activity activity) => new(activity.Id, activity.Title,
        activity.Description, activity.Date, activity.Type);
}
