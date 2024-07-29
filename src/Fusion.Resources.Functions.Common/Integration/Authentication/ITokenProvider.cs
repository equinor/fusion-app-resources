namespace Fusion.Resources.Functions.Common.Integration.Authentication
{
    public interface ITokenProvider
    {
        /// <summary>
        /// Get access token to the current azure ad app.
        /// </summary>
        /// <returns></returns>
        Task<string> GetAppAccessToken();

        /// <summary>
        /// Get an application access token to the specified resource.
        /// </summary>
        /// <param name="resource">The external service resource identifier, normally the base url</param>
        /// <returns></returns>
        Task<string> GetAppAccessToken(string resource);
    }

}
