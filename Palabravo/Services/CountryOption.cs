using System.Globalization;

namespace Palabravo.Services;

public sealed record CountryOption(string Code, string Label)
{
    public static IReadOnlyList<CountryOption> All { get; } = Build();
    public static string Flag(string code) => code.Length == 2 && code.All(c => c is >= 'A' and <= 'Z')
        ? string.Concat(code.Select(c => char.ConvertFromUtf32(0x1F1E6 + c - 'A'))) : "";

    private static IReadOnlyList<CountryOption> Build()
    {
        var countries = new Dictionary<string, CountryOption>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                var code = region.TwoLetterISORegionName;
                if (code.Length == 2 && code.All(c => c is >= 'A' and <= 'Z'))
                    countries.TryAdd(code, new(code, $"{Flag(code)} {region.DisplayName}"));
            }
            catch (ArgumentException) { }
        }
        return new[] { new CountryOption("", "Sin bandera") }
            .Concat(countries.Values.OrderBy(c => c.Label, StringComparer.CurrentCulture)).ToArray();
    }
}
