// See https://aka.ms/new-console-template for more information
using System.Reflection;

public class DefaultUtils : IUtils
{
    public PipelineInformation? ResolvePipelineFromEnvironment()
    {
        switch (ResolvePipelineEnvironment())
        {
            case PipelineEnvironment.DevOps:
                return ResolveDevOpsRun();

            case PipelineEnvironment.GitHub:
                return ResolveGithubRun();

            default:
                return null;

        }

    }

    public Version? GetCliVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }

    private PipelineEnvironment ResolvePipelineEnvironment()
    {
        // Look for flags indicating devops
        if (Environment.GetEnvironmentVariable("AGENT_ROOTDIRECTORY") is not null)
            return PipelineEnvironment.DevOps;

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            return PipelineEnvironment.GitHub;

        return PipelineEnvironment.Unknown;
    }

    private PipelineInformation ResolveDevOpsRun()
    {
        var runId = Environment.GetEnvironmentVariable("BUILD_BUILDID");
        var jobName = Environment.GetEnvironmentVariable("BUILD_DEFINITIONNAME");
        var repo = Environment.GetEnvironmentVariable("BUILD_REPOSITORY_ID");
        var branch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME");
        var jobId = Environment.GetEnvironmentVariable("SYSTEM_JOBID");
        var taskInstanceId = Environment.GetEnvironmentVariable("SYSTEM_TASKINSTANCEID");
        var commitId = Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION");

        var orgUrl = Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI");
        var orgProject = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECT");

        var link = $"{orgUrl?.TrimEnd('/')}/{orgProject}/_build/results?buildId={runId}&view=logs&j={jobId}&t={taskInstanceId}";

        return new PipelineInformation()
        {
            JobName = jobName,
            Repository = repo,
            RunId = runId,
            RunLink = link,
            BranchName = branch,
            Commit = commitId,
            PipelineEnvironment = PipelineEnvironment.DevOps
        };
    }

    private PipelineInformation ResolveGithubRun()
    {
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        var runNumber = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
        var jobName = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW");
        var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var branch = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
        var commitId = Environment.GetEnvironmentVariable("GITHUB_SHA");
        
        var orgUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");

        //https://github.com/equinor/fusion-core-apps/actions/runs/9792809509

        var link = $"{orgUrl?.TrimEnd('/')}/{repo?.TrimEnd('/')}/actions/runs/{runNumber}";

        return new PipelineInformation()
        {
            JobName = jobName,
            Repository = repo,
            RunId = runId,
            RunLink = link,
            BranchName = branch,
            Commit = commitId,
            PipelineEnvironment = PipelineEnvironment.GitHub
        };
    }
}
