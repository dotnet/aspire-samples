namespace ConsoleApp.Model;

internal class PokemonList
{
    public int Count { get; init; }
    public required string? Next { get; init; }
    public required string? Previous { get; init; }
    public required List<Pokemon> Results { get; init; }
}
