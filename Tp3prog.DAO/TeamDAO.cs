using Tp3prog.Entities;
using Tp3prog.DAO.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Tp3prog.DAO;

public class TeamDAO
{
    private readonly AppDbContext context;

    public TeamDAO(AppDbContext context)
    {
        this.context = context;
    }

    public List<Team> GetAll() => context.Teams.AsNoTracking()
        .Include(team => team.Players).Include(team => team.Trainers).AsSplitQuery().ToList();

    public Team? GetById(long id) => context.Teams
        .Include(team => team.Players).Include(team => team.Trainers).AsSplitQuery()
        .SingleOrDefault(team => team.Id == id);

    public RelationshipResult Create(Team team, IReadOnlyCollection<long>? playerIds)
    {
        if (playerIds is null || playerIds.Count == 0) return RelationshipResult.MinimumRequired;
        var ids = playerIds.Distinct().ToArray();
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, ids);
        var players = context.Players.Where(player => ids.Contains(player.Id)).ToList();
        if (players.Count != ids.Length) return RelationshipResult.NotFound;
        team.Players = players;
        context.Teams.Add(team);
        context.SaveChanges();
        transaction.Commit();
        return RelationshipResult.Success;
    }

    public bool Update(long id, Team team)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        existing.Name = team.Name;
        existing.Category = team.Category;
        context.SaveChanges();
        return true;
    }

    public bool Delete(long id)
    {
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.Teams(context, [id]);
        var team = GetById(id);
        if (team is null) return false;
        context.Teams.Remove(team);
        context.SaveChanges();
        transaction.Commit();
        return true;
    }

    public RelationshipResult AddPlayer(long id, long playerId)
    {
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, [playerId]);
        MembershipLocks.Teams(context, [id]);
        var team = GetById(id);
        var player = context.Players.Find(playerId);
        if (team is null || player is null) return RelationshipResult.NotFound;
        if (!context.Teams.Any(existing => existing.Id == id && existing.Players.Any(member => member.Id == playerId)))
        {
            team.Players.Add(player);
            context.SaveChanges();
        }
        transaction.Commit();
        return RelationshipResult.Success;
    }

    public RelationshipResult RemovePlayer(long id, long playerId)
    {
        using var transaction = MembershipLocks.Begin(context);
        MembershipLocks.People(context, [playerId]);
        MembershipLocks.Teams(context, [id]);
        if (!context.Teams.Any(team => team.Id == id) || !context.Players.Any(player => player.Id == playerId))
            return RelationshipResult.NotFound;
        var count = context.Teams.Where(team => team.Id == id && team.Players.Any(player => player.Id == playerId))
            .Select(team => (int?)team.Players.Count).SingleOrDefault();
        if (count is null) return RelationshipResult.Success;
        if (count <= 1) return RelationshipResult.MinimumRequired;
        var team = GetById(id);
        var assigned = team!.Players.Single(existing => existing.Id == playerId);
        team.Players.Remove(assigned);
        context.SaveChanges();
        transaction.Commit();
        return RelationshipResult.Success;
    }

    public RelationshipResult AddTrainer(long id, long trainerId)
    {
        var team = GetById(id);
        var trainer = context.Trainers.Find(trainerId);
        if (team is null || trainer is null) return RelationshipResult.NotFound;
        if (team.Trainers.All(existing => existing.Id != trainerId))
        {
            team.Trainers.Add(trainer);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }

    public RelationshipResult RemoveTrainer(long id, long trainerId)
    {
        var team = GetById(id);
        var trainer = context.Trainers.Find(trainerId);
        if (team is null || trainer is null) return RelationshipResult.NotFound;
        var assigned = team.Trainers.SingleOrDefault(existing => existing.Id == trainerId);
        if (assigned is not null)
        {
            team.Trainers.Remove(assigned);
            context.SaveChanges();
        }
        return RelationshipResult.Success;
    }
}
