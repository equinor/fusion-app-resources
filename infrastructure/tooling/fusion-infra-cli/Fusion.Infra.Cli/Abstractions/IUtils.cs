// See https://aka.ms/new-console-template for more information
/// <summary>
/// In lack of a better name, a place to put general utils so it can be mocked and tested.
/// </summary>
public interface IUtils
{
    Version? GetCliVersion();
    PipelineInformation? ResolvePipelineFromEnvironment();
}
