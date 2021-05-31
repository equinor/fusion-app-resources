namespace Fusion.Resources.Logic.Requests
{
    public record WorkflowAccessKey(string Subtype, string CurrentStep)
    {
        public static implicit operator WorkflowAccessKey((string, string) tuple)
            => new WorkflowAccessKey(tuple.Item1, tuple.Item2);
    }
}
