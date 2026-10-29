namespace RatRaceEscape.Engine.Tests;

public class GameSeedTests
{
    [Fact]
    public void The_same_seed_repeats_the_dice_and_ignores_the_injected_roller()
    {
        Game first = Play(42);
        Game second = Play(42);
        Game other = Play(99);

        Assert.Equal(42, first.Seed);
        Assert.Equal(first.LastRoll!.Faces, second.LastRoll!.Faces);
        Assert.Equal(first.CurrentPlayer.BoardIndex, second.CurrentPlayer.BoardIndex);
        Assert.Equal(first.CurrentPlayer.Cash, second.CurrentPlayer.Cash);
        Assert.Equal(first.DiceConsumed, second.DiceConsumed);
        Assert.Equal(first.Piles.SmallDeals.Draw().Card.Id, second.Piles.SmallDeals.Draw().Card.Id);
        bool sameFace = first.LastRoll.Faces[0] == other.LastRoll!.Faces[0];
        bool sameCard = first.Piles.SmallDeals.Draw().Card.Id == other.Piles.SmallDeals.Draw().Card.Id;
        Assert.False(sameFace && sameCard);
    }

    [Fact]
    public void A_second_roll_continues_the_seeded_sequence()
    {
        Game once = Open(7).Roll(new ScriptedDieRoller([1]));
        Game twice = Advance(once).Roll(new ScriptedDieRoller([6]));
        IDieRoller dice = DiceSequence.After(7, 0);

        Assert.Equal([dice.Roll()], once.LastRoll!.Faces);
        Assert.Equal([dice.Roll()], twice.LastRoll!.Faces);
        Assert.Equal(2, twice.DiceConsumed);
    }

    private static Game Advance(Game rolled)
    {
        SpaceKind kind = RatRaceBoard.At(rolled.CurrentPlayer.BoardIndex).Kind;
        SpaceChoice choice = kind switch
        {
            SpaceKind.Charity => SpaceChoice.Decline,
            SpaceKind.Deal => SpaceChoice.SmallDeal,
            _ => SpaceChoice.None,
        };
        Game resolved;
        try
        {
            resolved = rolled.Resolve(choice);
        }
        catch (TurnException)
        {
            resolved = rolled.Settle();
        }

        if (!resolved.CurrentPlayer.IsBankrupt && resolved.Drawn is not null)
        {
            resolved = resolved.Pass();
        }

        return resolved.EndTurn();
    }

    private static Game Play(int seed)
    {
        Game game = Open(seed);
        Game rolled = game.Roll(new ScriptedDieRoller([1]));
        SpaceKind kind = RatRaceBoard.At(rolled.CurrentPlayer.BoardIndex).Kind;
        SpaceChoice choice = kind switch
        {
            SpaceKind.Charity => SpaceChoice.Decline,
            SpaceKind.Deal => SpaceChoice.SmallDeal,
            _ => SpaceChoice.None,
        };
        try
        {
            return rolled.Resolve(choice);
        }
        catch (TurnException)
        {
            return rolled.Settle();
        }
    }

    private static Game Open(int seed)
    {
        return Game.Create([new PlayerSetup("Ava", "night-baker", TokenColour.Amber)], seed);
    }
}
