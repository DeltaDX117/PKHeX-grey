using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PKHeX.Core;

/// <summary>
/// Generation 5 <see cref="SaveFile"/> object for <see cref="GameVersion.B2W2"/>.
/// </summary>
/// <inheritdoc cref="SAV5" />
public sealed class SAV5B2W2 : SAV5, ISaveBlock5B2W2
{
    public SAV5B2W2() : base(SaveUtil.SIZE_G5RAW) => Blocks = new SaveBlockAccessor5B2W2(this);
    public SAV5B2W2(Memory<byte> data) : this(data, false) { }

    /// <param name="data">The save file's data.</param>
    /// <param name="isGrey">Whether the save is of Pokémon Grey, a Black 2/White 2 ROM hack with a bigger main save.</param>
    public SAV5B2W2(Memory<byte> data, bool isGrey) : base(data)
    {
        IsGrey = isGrey;
        Blocks = new SaveBlockAccessor5B2W2(this);
    }

    /// <summary>
    /// Whether the save is of Pokémon Grey, a Black 2/White 2 ROM hack. Its main save is 0x600 bigger, for a Pokédex of
    /// 2047 species, so the blocks after the Pokédex are 0x600 later (<see cref="SaveBlockAccessor5B2W2"/>), the extra
    /// sections 0x1000 later, and the data kept outside the save at 0x7F100.
    /// </summary>
    public bool IsGrey { get; }

    /// <summary>How much later Pokémon Grey's main save blocks after its Pokédex are.</summary>
    public const int GreyMainShift = 0x600;
    /// <summary>How much later Pokémon Grey's extra sections are, after its two bigger copies of the main save.</summary>
    public const int GreyExtShift = 0x1000;
    private int ExtShift => IsGrey ? GreyExtShift : 0;

    public override PersonalTable5B2W2 Personal => IsGrey ? GreyData.Instance.Personal : PersonalTable.B2W2;
    public override ushort MaxSpeciesID => IsGrey ? GreyData.Instance.MaxSpeciesID : base.MaxSpeciesID;
    public override ushort MaxMoveID => IsGrey ? GreyData.Instance.MaxMoveID : base.MaxMoveID;
    public override int MaxAbilityID => IsGrey ? GreyData.Instance.MaxAbilityID : base.MaxAbilityID;
    public SaveBlockAccessor5B2W2 Blocks { get; }
    protected override SAV5B2W2 CloneInternal() => new(Data.ToArray(), IsGrey);
    public override int MaxItemID => IsGrey ? GreyData.Instance.MaxItemID : Legal.MaxItemID_5_B2W2;

    public override PlayerBag5B2W2 Inventory => new(this);

    public override IReadOnlyList<BlockInfo> AllBlocks => Blocks.BlockInfo;
    public override MyItem5B2W2 Items => Blocks.Items;
    public override Zukan5 Zukan => Blocks.Zukan;
    public override Misc5 Misc => Blocks.Misc;
    public override MysteryBlock5 Mystery => Blocks.Mystery;
    public override Chatter5 Chatter => Blocks.Chatter;
    public override Daycare5 Daycare => Blocks.Daycare;
    public override BoxLayout5 BoxLayout => Blocks.BoxLayout;
    public override PlayerData5B2W2 PlayerData => Blocks.PlayerData;
    public override PlayerPosition5 PlayerPosition => Blocks.PlayerPosition;
    public override BattleSubwayPlay5 BattleSubwayPlay => Blocks.BattleSubwayPlay;
    public override BattleSubway5 BattleSubway => Blocks.BattleSubway;
    public override Entralink5 Entralink => Blocks.Entralink;
    public override Musical5 Musical => Blocks.Musical;
    public override Encount5 Encount => Blocks.Encount;
    public override UnityTower5 UnityTower => Blocks.UnityTower;
    public override SkinInfo5 SkinInfo => Blocks.SkinInfo;
    public override EventWork5B2W2 EventWork => Blocks.EventWork;
    public override BattleBox5 BattleBox => Blocks.BattleBox;
    public override EntreeForest EntreeForest => Blocks.EntreeForest;
    public override GlobalLink5 GlobalLink => Blocks.GlobalLink;
    public override GTS5 GTS => Blocks.GTS;
    public override WhiteBlack5B2W2 Forest => Blocks.Forest;
    public override AdventureInfo5 AdventureInfo => Blocks.AdventureInfo;
    public override Record5 Records => Blocks.Records;

    public FestaBlock5 Festa => Blocks.Festa;
    public PWTBlock5 PWT => Blocks.PWT;
    public JoinAvenue5 JoinAvenue => Blocks.JoinAvenue;
    public MedalList5 Medals => Blocks.Medals;
    public KeySystem5 Keys => Blocks.Keys;

    public string RivalName
    {
        get => GetString(RivalNameTrash);
        set => SetString(RivalNameTrash, value, MaxStringLengthTrainer, StringConverterOption.ClearZero);
    }

    public Span<byte> RivalNameTrash
    {
        get => Data.Slice(RivalNameOffset, MaxStringLengthTrainer * 2);
        set { if (value.Length == MaxStringLengthTrainer * 2) value.CopyTo(Data[RivalNameOffset..]); }
    }

    // In the Rival/Hollow block, after the Pokédex
    private int RivalNameOffset => 0x23BA4 + (IsGrey ? GreyMainShift : 0);

    protected override int ExtBattleVideoNativeOffset    => 0x4C000 + ExtShift; // 0x1A00
    protected override int ExtBattleVideoDownload1Offset => 0x4DA00 + ExtShift; // 0x1A00
    protected override int ExtBattleVideoDownload2Offset => 0x4F400 + ExtShift; // 0x1A00
    protected override int ExtBattleVideoDownload3Offset => 0x50E00 + ExtShift; // 0x1A00
    protected override int ExtCGearOffset => 0x52800 + ExtShift; // 0x2800
    protected override int ExtBattleTestOffset => 0x55000 + ExtShift; // 0x800
    protected override int ExtMusicalDownloadOffset => 0x55800 + ExtShift; // 0x18000
    protected override int ExtPokeDexSkinOffset => 0x6D800 + ExtShift; // 0x6800
    protected override int ExtHallOfFame1Offset => 0x74000 + ExtShift; // 0x1800
    protected override int ExtHallOfFame2Offset => 0x75800 + ExtShift; // 0x1800

    public const int PokestarOffset = 0x77000; // 0x600 * 8
    public const int PWTOffset = 0x7A000; // 0x1400 * 3

    public const int KeyDataOffset = 0x7DC00; // 0x400, size 0xC8

    // Pokémon Grey keeps these past Pokémon Dream Radar's data at 0x7F000, which can't move
    protected override int ExtLink1Offset => IsGrey ? 0x7F100 : 0x7E000; // 0x400
    protected override int ExtLink2Offset => IsGrey ? 0x7F500 : 0x7E400; // 0x400

    public override int MusicalDownloadSize => MusicalShow5.SIZE_B2W2;
    public const int ExtUnk7E800 = 0x7E800; // Key System?
    public const int ExtUnkCRGF = 0x7F000; // [0..F] => checksum @ 0x10

    // B2/W2 have Pokéstar Studios and PWT DLC data too.
    public const int PokestarCount = 8;
    public const int PokestarLength   = PokestarMovie5.SIZE;
    public const int PokestarInterval = 0x600;
    private int GetPokestarOffset(int index) => PokestarOffset + ExtShift + (index * PokestarInterval);

    public Memory<byte> GetPokestarMovie([Range(0, PokestarCount)] int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, PokestarCount);
        return Buffer.Slice(GetPokestarOffset(index), PokestarLength);
    }

    public void SetPokestarMovie([Range(0, PokestarCount)] int index, ReadOnlySpan<byte> data, ushort count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, PokestarCount);
        var offset = GetPokestarOffset(index);
        WriteExtSection(data, offset, PokestarLength, count);
        PlayerData.UpdateExtData(ExtDataSectionNote5.Movie1 + index, count);
    }

    public const int PWTCount = 3;
    public const int PWTLength   = 0x1214;
    public const int PWTInterval = 0x1400;
    private int GetPWTOffset(int index) => PWTOffset + ExtShift + (index * PWTInterval);

    public Memory<byte> GetPWT([Range(0, PWTCount)] int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, PWTCount);
        return Buffer.Slice(GetPWTOffset(index), PWTLength);
    }

    public void SetPWT([Range(0, PWTCount)] int index, ReadOnlySpan<byte> data, ushort count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, PWTCount);
        var offset = GetPWTOffset(index);
        WriteExtSection(data, offset, PWTLength, count);
        PlayerData.UpdateExtData(ExtDataSectionNote5.PWT1 + index, count);
    }

    public Memory<byte> GetKeyData() => Buffer.Slice(KeyDataOffset + ExtShift, 0xC8);

    public void SetKeyData(ReadOnlySpan<byte> data, ushort count = 1)
    {
        PlayerData.UpdateExtData(ExtDataSectionNote5.KeyData, count);
        WriteExtSection(data, KeyDataOffset + ExtShift, PWTLength, count);
    }
}
