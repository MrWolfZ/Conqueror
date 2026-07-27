namespace Conqueror.Recipes.Iterating.GettingStarted;

using System.Runtime.CompilerServices;

internal partial class GetCitiesHandler : GetCities.IHandler
{
    private static readonly Dictionary<string, string[]> CitiesByCountry = new()
    {
        ["at"] = ["Vienna", "Graz", "Linz"],
        ["de"] = ["Berlin", "Munich", "Hamburg"],
    };

    public async IAsyncEnumerable<string> Handle(
        GetCities iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var cities = CitiesByCountry.GetValueOrDefault(iterator.Country) ?? [];

        foreach (var city in cities)
        {
            // simulate fetching each item from a slow data source; the consumer only pays
            // for the items it actually pulls
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            yield return city;
        }
    }
}
