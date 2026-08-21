namespace CodePlus.Models;

public sealed record ProjectSummary(string Name, string Language, string Status, double Progress);

public sealed record Snippet(string Title, string Language, string Code, string Tags);
