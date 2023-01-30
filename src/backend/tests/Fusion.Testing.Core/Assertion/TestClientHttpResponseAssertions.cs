using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Fusion.Testing
{
    public class TestClientHttpResponseAssertions<T> : ReferenceTypeAssertions<TestClientHttpResponse<T>, TestClientHttpResponseAssertions<T>>
    {
        public TestClientHttpResponseAssertions(TestClientHttpResponse<T> instance) : base(instance)
        {
        }

        protected override string Identifier => "directory";

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeSuccessfull(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.IsSuccessStatusCode)
                .FailWith($"Expected successfull http response but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.Method}: {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> HaveAllowHeaders(params HttpMethod[] methods)
        {

            var containsHeaders = false;

            List<string> allowedMethods = new List<string>();

            if (Subject.Response.Content.Headers.TryGetValues("Allow", out IEnumerable<string> allowedHeaders))
            {
                foreach (var header in allowedHeaders)
                    allowedMethods.AddRange(header.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            containsHeaders = methods.Any(h => allowedMethods.Contains(h.Method, StringComparer.OrdinalIgnoreCase));

            Execute.Assertion
                .BecauseOf("")
                .ForCondition(containsHeaders)
                .FailWith($"Expected Allow header to contain {string.Join(", ", methods.Select(m => m.ToString()))} but found {string.Join(", ", allowedMethods)}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> NotHaveAllowHeaders(params HttpMethod[] methods)
        {
            var containsHeaders = false;

            List<string> allowedMethods = new List<string>();

            if (Subject.Response.Content.Headers.TryGetValues("Allow", out IEnumerable<string> allowedHeaders))
            {
                foreach (var header in allowedHeaders)
                    allowedMethods.AddRange(header.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));                
            }

            containsHeaders = methods.Any(h => allowedMethods.Contains(h.Method, StringComparer.OrdinalIgnoreCase));

            Execute.Assertion
                .BecauseOf("")
                .ForCondition(!containsHeaders)
                .FailWith($"Expected Allow header to contain {string.Join(", ", methods.Select(m => m.ToString()))} but found {string.Join(", ", allowedMethods)}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }


        public AndConstraint<TestClientHttpResponseAssertions<T>> BeUnauthorized(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.Unauthorized || Subject.Response.StatusCode == HttpStatusCode.Forbidden)
                .FailWith($"Expected Unauthorized, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeBadRequest(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.BadRequest)
                .FailWith($"Expected BadRequest, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeConflict(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.Conflict)
                .FailWith($"Expected Conflict, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeNotFound(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.NotFound)
                .FailWith($"Expected NotFound, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeNotModified(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.NotModified)
                .FailWith($"Expected NotModified, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeCreated(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.Created)
                .FailWith($"Expected Created, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeNoContent(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.NoContent)
                .FailWith($"Expected NoContent, but found {Subject.Response.StatusCode}. {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeFailedDependency(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition((int)Subject.Response.StatusCode == 424)
                .FailWith($"Expected Failed Dependency (424), but found {Subject.Response.StatusCode} ({(int)Subject.Response.StatusCode}). {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> BeAccepted(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.Accepted)
                .FailWith($"Expected Accepted (202), but found {Subject.Response.StatusCode} ({(int)Subject.Response.StatusCode}). {Subject.Response.RequestMessage.RequestUri}");

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }

        public AndConstraint<TestClientHttpResponseAssertions<T>> ContainErrorOnProperty(string propertyName, string because = "", params object[] becauseArgs)
        {
            var validationErrors = new Dictionary<string, string[]>();

            try
            {
                var errorsResponse = JsonConvert.DeserializeAnonymousType(Subject.Content, new { errors = new Dictionary<string, string[]>() });
                validationErrors = errorsResponse.errors;
            } catch { TestLogger.TryLog("Could not deserialize validation errors"); }

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(Subject.Response.StatusCode == HttpStatusCode.BadRequest && validationErrors.Keys.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                .FailWith("Expected BadRequest response to contain property, but found {0} and invalid properties {1}", Subject.Response.StatusCode, JsonConvert.SerializeObject(validationErrors));

            return new AndConstraint<TestClientHttpResponseAssertions<T>>(this);
        }
    }

}
