namespace MealPreparator.Infrastructure.Persistence;

public static class DatabasePathResolver
{
    private static readonly string[] SolutionMarkers =
    [
        "MealPreparator.sln",
        "MealPreparator.slnx"
    ];

    public static string Resolve(string dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new ArgumentException("Database data source cannot be empty.", nameof(dataSource));
        }

        if (Path.IsPathRooted(dataSource))
        {
            return dataSource;
        }

        var startDirectories = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var startDirectory in startDirectories)
        {
            var solutionRoot = FindSolutionRoot(startDirectory);

            if (solutionRoot is not null)
            {
                return Path.GetFullPath(Path.Combine(solutionRoot, dataSource));
            }
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), dataSource));
    }

    private static string? FindSolutionRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            if (SolutionMarkers.Any(marker =>
                    File.Exists(Path.Combine(directory.FullName, marker))))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
