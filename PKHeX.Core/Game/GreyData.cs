using System;
using System.IO;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// The data of Pokémon Grey, a Black 2/White 2 ROM hack with more species than the 649 of Generation 5: its species'
/// records, which species numbers exist, their numbers in its Pokédex, and its last move, ability and item.
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
    }

    /// <summary>Whether the species number has a species: Grey's numbers follow the official ones, with free numbers between.</summary>
    public bool IsSpeciesPresent(ushort species) => species < Present.Length && Present[species];

    /// <summary>The species' number in Grey's Pokédex, or 0 if the Pokédex doesn't list it.</summary>
    public ushort GetDexNumber(ushort species) => species < DexNumbers.Length ? DexNumbers[species] : (ushort)0;

    private static Memory<byte> Section(Memory<byte> data, ReadOnlySpan<char> tag)
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
            return data.Slice(offset, size);
        }
        throw new ArgumentException($"Pokémon Grey's data has no {tag} section.", nameof(data));
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
