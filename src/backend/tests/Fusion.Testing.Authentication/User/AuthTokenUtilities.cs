using Newtonsoft.Json;
using System;
using System.Text;

namespace Fusion.Testing.Authentication.User
{
    public static class AuthTokenUtilities
    {
        /// <summary>
        /// Converts <paramref name="bearerToken"/> from base64, UTF-8 encoded string and then deserializes it to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type you wish to attempt to deserialize to</typeparam>
        /// <param name="bearerToken">The token as it appears in the Authorization header</param>
        /// <param name="anonymousType">Instance of the anonymous type you are deserializing to</param>
        /// <returns>Deserialized object of type <typeparamref name="T"/></returns>
        public static T UnwrapAuthToken<T>(string bearerToken, T anonymousType) where T : class
        {
            var tokenPart = bearerToken.Split(' ')[1];
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tokenPart));
            var deserialized = JsonConvert.DeserializeAnonymousType(decoded, anonymousType);

            return deserialized;
        }

        /// <summary>
        /// Wraps <typeparamref name="T"/> by serializing, encoding and then converting to base 64 string. 
        /// "Bearer" is also added, making it ready to be added as Authorization header
        /// </summary>
        /// <typeparam name="T">The type of the class that is serialized</typeparam>
        /// <param name="tokenClass">The instance of the token to be wrapped</param>
        /// <returns>Serialized, encoded string ready for authorization header</returns>
        public static string WrapAuthToken<T>(T tokenClass) where T : class
        {
            var serialized = JsonConvert.SerializeObject(tokenClass);
            var tokenBytes = Encoding.UTF8.GetBytes(serialized);
            var tokenString = Convert.ToBase64String(tokenBytes);

            return $"Bearer {tokenString}";
        }
    }
}
