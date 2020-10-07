namespace Fusion.Testing
{
    public static class TestClientHttpResponseExtensions
    {
        public static TestClientHttpResponseAssertions<T> Should<T>(this TestClientHttpResponse<T> instance)
        {
            return new TestClientHttpResponseAssertions<T>(instance);
        }
    }

}
