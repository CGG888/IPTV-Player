namespace LibmpvIptvClient.Architecture.Tooling;

public sealed record BuildTask(string Name, string Command, string WorkingDirectory);

public sealed class BuildPipelinePlanner
{
    public IReadOnlyList<BuildTask> CreateDefault(string projectRoot)
    {
        return
        [
            new BuildTask("Restore", "dotnet restore", projectRoot),
            new BuildTask("Build", "dotnet build", projectRoot),
            new BuildTask("Test", "dotnet test", projectRoot)
        ];
    }
}
