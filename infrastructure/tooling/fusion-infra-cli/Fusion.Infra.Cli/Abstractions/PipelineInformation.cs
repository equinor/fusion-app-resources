// See https://aka.ms/new-console-template for more information
public struct PipelineInformation
{
    /// <summary>
    /// An identifier for the automation job that is running.
    /// </summary>
    public string? RunId { get; set; }
    /// <summary>
    /// Name of the repository, including organisation.
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// Link that can open information about the executed pipeline/action/automation job.
    /// </summary>
    public string? RunLink { get; set; }

    /// <summary>
    /// Display name of the pipeline/automation job
    /// </summary>
    public string? JobName { get; set; }

    /// <summary>
    /// Branch code is executed from.
    /// </summary>
    public string? BranchName { get; set; }

    public PipelineEnvironment PipelineEnvironment { get; set; }
    public string? Commit { get; internal set; }
}
