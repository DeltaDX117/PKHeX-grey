using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Entity;

public static class GreyAbilityTests
{
    [Fact]
    public static void GreyAbilityKeepsItsHighByte()
    {
        var pk = new PK5();
        GreyData.Active = true;
        try
        {
            pk.Ability = 306;
            pk.Data[0x15].Should().Be(306 & 0xFF);
            pk.Data[0x43].Should().Be(306 >> 8);
            pk.Ability.Should().Be(306);
        }
        finally
        {
            GreyData.Active = false;
        }
        pk.Ability.Should().Be(306 & 0xFF);
    }

    [Fact]
    public static void GreyDataHasEveryRecordsAbilities()
    {
        var personal = GreyData.Instance.Personal;
        GreyData.Instance.MaxAbilityID.Should().BeGreaterThan(255);
        // Reshiram's: Turboblaze, which a byte holds the same, read through the full abilities
        personal.GetFormEntry((ushort)Species.Reshiram, 0).Ability1.Should().Be((int)Ability.Turboblaze);
        GameInfo.Strings.abilitylist.Length.Should().BeGreaterThan(GreyData.Instance.MaxAbilityID);
    }
}
