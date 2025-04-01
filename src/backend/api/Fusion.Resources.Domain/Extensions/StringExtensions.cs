namespace Fusion.Resources.Domain
{
    public static class StringExtensions
    {
        /// <summary>
        /// Ensure the project state is valid. If the state is null or empty it should be set to active,
        /// otherwise retain the passed in state.
        /// </summary>
        public static string ResolveProjectState(this string? state)
        {
            if (string.IsNullOrEmpty(state))
                return "ACTIVE";

            return state;
        }
    }
}
