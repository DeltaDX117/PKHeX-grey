using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// The data of Pokémon Grey, a Black 2/White 2 ROM hack with more species than the 649 of Generation 5: its species'
/// records, which species numbers exist, their numbers in its Pokédex, its last move, ability and item, each move's
/// type and PP, and the names of its moves, abilities and items.
/// </summary>
/// <remarks>
/// The hack's build writes the file (tools/data/pkhex_export.py there, which describes it). It is read from
/// <see cref="FileName"/> beside the program, so that the editor follows the hack without a new build, or else from
/// the copy built into this assembly.
/// </remarks>
public sealed class GreyData
{
    /// <summary>The file beside the program that replaces the built-in data.</summary>
    public const string FileName = "grey.pkhex";
    private const string ResourceName = "grey_data";
    private const uint Magic = 0x59455247; // "GREY"

    /// <summary>
    /// Whether the save file being edited is Pokémon Grey's, so that its <see cref="PK5"/> take Grey's limits and
    /// records. An entity doesn't know its save file, so this is set with the save file that the program loads
    /// (<see cref="ParseSettings.InitFromSaveFileData"/>).
    /// </summary>
    public static bool Active { get; set; }

    private static readonly Lazy<GreyData> Loaded = new(Load);
    public static GreyData Instance => Loaded.Value;

    /// <summary>The last species number a Pokémon can have, before the egg's.</summary>
    public ushort MaxSpeciesID { get; }
    public ushort MaxMoveID { get; }
    public ushort MaxAbilityID { get; }
    public ushort MaxItemID { get; }
    public PersonalTable5B2W2 Personal { get; }

    // Version 3: each move's type and PP, and the names of the moves, abilities and items, empty where Grey has none
    private readonly byte[] MoveTypes = [];
    private readonly byte[] MovePP = [];
    private readonly string[] MoveNames = [];
    private readonly string[] AbilityNames = [];
    private readonly string[] ItemNames = [];

    /// <summary>The move's PP in Grey, or null where the data has none (an older file).</summary>
    public byte? GetPP(ushort move) => move < MovePP.Length && MovePP[move] != 0 ? MovePP[move] : null;

    /// <summary>
    /// The move's type in Grey, or null where the data has none. Grey's ??? and Shadow types, which PKHeX has no names
    /// for, read as Normal.
    /// </summary>
    public byte? GetMoveType(ushort move)
    {
        if (move >= MoveTypes.Length || (MovePP.Length > move && MovePP[move] == 0))
            return null;
        var type = MoveTypes[move];
        return type <= (byte)MoveType.Fairy + 1 ? type : (byte)0; // Up to Stellar
    }

    /// <summary>Where the data was read from: the file's path, or null for the built-in copy.</summary>
    public string? SourcePath { get; }

    private readonly bool[] Present;
    private readonly ushort[] DexNumbers;

    private GreyData(Memory<byte> data, string? path)
    {
        SourcePath = path;
        var span = data.Span;
        if (span.Length < 8 || ReadUInt32LittleEndian(span) != Magic)
            throw new ArgumentException("Not Pokémon Grey's data.", nameof(data));

        var limits = Section(data, "LIMS").Span;
        MaxSpeciesID = ReadUInt16LittleEndian(limits);
        var speciesCount = ReadUInt16LittleEndian(limits[2..]);
        MaxMoveID = ReadUInt16LittleEndian(limits[4..]);
        MaxAbilityID = ReadUInt16LittleEndian(limits[6..]);
        MaxItemID = ReadUInt16LittleEndian(limits[8..]);

        Present = new bool[speciesCount];
        var species = Section(data, "SPEC").Span;
        for (int i = 0; i < species.Length; i += 2)
            Present[ReadUInt16LittleEndian(species[i..])] = true;

        DexNumbers = new ushort[speciesCount];
        var numbers = Section(data, "DEXN").Span;
        for (int i = 0; i < speciesCount; i++)
            DexNumbers[i] = ReadUInt16LittleEndian(numbers[(i * 2)..]);

        Personal = new PersonalTable5B2W2(Section(data, "PERS"), MaxSpeciesID, Present);

        if (TrySection(data, "MOVE", out var moves))
        {
            var span2 = moves.Span;
            MoveTypes = new byte[span2.Length / 2];
            MovePP = new byte[span2.Length / 2];
            for (int i = 0; i < MoveTypes.Length; i++)
            {
                MoveTypes[i] = span2[i * 2];
                MovePP[i] = span2[(i * 2) + 1];
            }
        }
        if (TrySection(data, "NAMM", out var moveNames))
            MoveNames = ReadNames(moveNames.Span);
        if (TrySection(data, "NAMA", out var abilityNames))
            AbilityNames = ReadNames(abilityNames.Span);
        if (TrySection(data, "NAMI", out var itemNames))
            ItemNames = ReadNames(itemNames.Span);

        // Version 2 has every record's abilities in full, which Grey keeps in 16 bits
        if (TrySection(data, "ABIL", out var wide))
        {
            var abilities = wide.Span;
            for (int i = 0; i < Personal.Count && (i * 6) + 6 <= abilities.Length; i++)
            {
                var entry = abilities[(i * 6)..];
                Personal[i].SetWideAbilities(ReadUInt16LittleEndian(entry), ReadUInt16LittleEndian(entry[2..]),
                    ReadUInt16LittleEndian(entry[4..]));
            }
        }
    }

    /// <summary>Whether the species number has a species: Grey's numbers follow the official ones, with free numbers between.</summary>
    public bool IsSpeciesPresent(ushort species) => species < Present.Length && Present[species];

    /// <summary>The species' number in Grey's Pokédex, or 0 if the Pokédex doesn't list it.</summary>
    public ushort GetDexNumber(ushort species) => species < DexNumbers.Length ? DexNumbers[species] : (ushort)0;

    private static string[] ReadNames(ReadOnlySpan<byte> span)
    {
        var names = new List<string>();
        int start = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] != 0)
                continue;
            names.Add(Encoding.UTF8.GetString(span[start..i]));
            start = i + 1;
        }
        return [.. names];
    }

    /// <summary>
    /// Lengthens a list of names to hold Grey's numbers past its end, with Grey's names there, so that the editor's
    /// lists have them (<see cref="GameStrings"/>).
    /// </summary>
    public static string[] Extend(string[] names, string kind)
    {
        var grey = Instance.GetNames(kind);
        if (grey.Length <= names.Length)
            return names;
        var result = new string[grey.Length];
        names.CopyTo(result, 0);
        for (int i = names.Length; i < grey.Length; i++)
            result[i] = grey[i];
        return result;
    }

    /// <summary>
    /// The items a Pokémon can hold in Grey: Black 2 and White 2's, and Grey's own in the numbers it reuses, which have
    /// a name of Grey's that isn't the official item's.
    /// </summary>
    public ushort[] HeldItems => heldItems ??= GetHeldItems();
    private ushort[]? heldItems;

    private ushort[] GetHeldItems()
    {
        var items = new SortedSet<ushort>(Legal.HeldItems_BW);
        var english = GameLanguage.GetStrings("items", "en");
        if (Originals.TryGetValue(english, out var englishOriginal))
            english = englishOriginal;
        for (int i = 1; i < ItemNames.Length && i <= MaxItemID; i++)
        {
            if (ItemNames[i].Length != 0 && (i >= english.Length || Letters(english[i]) != Letters(ItemNames[i])))
                items.Add((ushort)i);
        }
        return [.. items];
    }

    private string[] GetNames(string kind) => kind switch
    {
        "moves" => MoveNames,
        "abilities" => AbilityNames,
        "items" => ItemNames,
        _ => [],
    };

    // Each list's names as the program loaded them, to put back when a save file other than Grey's is loaded
    private static readonly ConditionalWeakTable<string[], string[]> Originals = new();

    /// <summary>
    /// Gives the lists of names Grey's names while its save file is the one being edited, where they differ from the
    /// program's English names: Grey's own moves, abilities and items, and the items in numbers it reuses. Other
    /// languages keep their names for everything Grey names as the official games do. With <paramref name="active"/>
    /// false, puts the program's names back.
    /// </summary>
    public static void ApplyNames(GameStrings strings, bool active)
    {
        var data = Instance;
        Apply(strings.movelist, data.MoveNames, "moves", active);
        Apply(strings.abilitylist, data.AbilityNames, "abilities", active);
        Apply(strings.itemlist, data.ItemNames, "items", active);
    }

    private static void Apply(string[] list, string[] grey, string kind, bool active)
    {
        if (!Originals.TryGetValue(list, out var original))
        {
            original = (string[])list.Clone();
            Originals.Add(list, original);
        }
        original.CopyTo(list, 0);
        if (!active)
            return;
        // The program's English names, before any of Grey's: the English lists are cached and may hold them now
        var english = GameLanguage.GetStrings(kind, "en");
        if (Originals.TryGetValue(english, out var englishOriginal))
            english = englishOriginal;
        for (int i = 0; i < list.Length && i < grey.Length; i++)
        {
            if (grey[i].Length != 0 && (i >= english.Length || Letters(english[i]) != Letters(grey[i])))
                list[i] = grey[i];
        }
    }

    // A name's letters and digits, so that the fifth generation's spellings (BrightPowder) match the later ones
    private static string Letters(string name)
    {
        var result = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
                result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    private static Memory<byte> Section(Memory<byte> data, ReadOnlySpan<char> tag)
    {
        if (TrySection(data, tag, out var section))
            return section;
        throw new ArgumentException($"Pokémon Grey's data has no {tag} section.", nameof(data));
    }

    private static bool TrySection(Memory<byte> data, ReadOnlySpan<char> tag, out Memory<byte> section)
    {
        var span = data.Span;
        int count = ReadUInt16LittleEndian(span[6..]);
        for (int i = 0; i < count; i++)
        {
            var entry = span.Slice(8 + (12 * i), 12);
            if (entry[0] != tag[0] || entry[1] != tag[1] || entry[2] != tag[2] || entry[3] != tag[3])
                continue;
            var offset = ReadInt32LittleEndian(entry[4..]);
            var size = ReadInt32LittleEndian(entry[8..]);
            section = data.Slice(offset, size);
            return true;
        }
        section = default;
        return false;
    }

    private static GreyData Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (File.Exists(path))
        {
            try
            {
                return new GreyData(File.ReadAllBytes(path), path);
            }
            // A file that can't be read or isn't the data falls back to the built-in copy; SourcePath then says so.
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or IndexOutOfRangeException)
            {
                System.Diagnostics.Debug.WriteLine($"{path}: {ex.Message}");
            }
        }
        return new GreyData(Util.GetBinaryResource(ResourceName), null);
    }
}
