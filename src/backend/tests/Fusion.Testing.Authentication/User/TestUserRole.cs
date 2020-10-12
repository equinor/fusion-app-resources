namespace Fusion.Testing.Authentication.User
{
    public sealed class TestUserRole
    {
        public string Name { get; set; }

        public static TestUserRole DevOps = new TestUserRole { Name = "ProView.Admin.DevOps" };
    }

}
