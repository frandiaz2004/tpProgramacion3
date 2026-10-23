namespace Tp3prog.Contracts;

public record TeamSummary(long Id, string Name, string Category);
public record CourseSummary(long Id, string Name);
public record PlayerSummary(long Id, string Name, int Age, string? Dni, int Number);
public record StudentSummary(long Id, string Name, int Age, string? Dni, string File);
public record TrainerSummary(long Id, string Name, int Age, string? Dni);
public record ActivityResponse(long Id, string Title, string Description, DateTime? Date, Entities.TypeActivity Type);

public record PlayerResponse(long Id, string Name, int Age, string? Dni, int Number, List<TeamSummary> Teams);
public record StudentResponse(long Id, string Name, int Age, string? Dni, string File, List<CourseSummary> Courses);
public record TrainerResponse(long Id, string Name, int Age, string? Dni, List<TeamSummary> Teams);
public record CourseResponse(long Id, string Name, List<StudentSummary> Students, List<ActivityResponse> Activities);
public record TeamResponse(long Id, string Name, string Category, List<PlayerSummary> Players, List<TrainerSummary> Trainers);
