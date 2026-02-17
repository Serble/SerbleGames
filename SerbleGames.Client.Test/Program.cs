using SerbleGames.Client;

Console.WriteLine("SerbleGames Client Library Test");

SerbleGamesClient client = new("http://localhost:5240");

try {
    Console.WriteLine("Fetching public games...");
    IEnumerable<Game>? publicGames = await client.GetPublicGames();
    if (publicGames != null) {
        foreach (Game game in publicGames) {
            Console.WriteLine($"- {game.Name} (ID: {game.Id}, Price: {game.Price})");
        }
    } else {
        Console.WriteLine("No public games found.");
    }
} catch (Exception ex) {
    Console.WriteLine($"Error fetching public games: {ex.Message}");
    Console.WriteLine("Make sure the backend is running at http://localhost:5240.");
}

Console.WriteLine("\nTest finished. To test OAuth, a real Serble app registration and interactive browser are needed.");
