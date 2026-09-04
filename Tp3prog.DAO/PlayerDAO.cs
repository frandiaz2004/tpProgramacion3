using Tp3prog.Entities;

namespace Tp3prog.DAO;

public class PlayerDAO
{
    public List<Player> GetAll() => MockDatabase.Players;

    public Player? GetById(long id) => MockDatabase.Players.FirstOrDefault(player => player.Id == id);

    public Player Create(Player player)
    {
        player.Id = MockDatabase.Players.Count == 0 ? 1 : MockDatabase.Players.Max(item => item.Id) + 1;
        MockDatabase.Players.Add(player);
        return player;
    }

    public bool Update(long id, Player player)
    {
        var existing = GetById(id);
        if (existing is null) return false;
        player.Id = id;
        MockDatabase.Players[MockDatabase.Players.IndexOf(existing)] = player;
        return true;
    }

    public bool Delete(long id)
    {
        var player = GetById(id);
        return player is not null && MockDatabase.Players.Remove(player);
    }
}