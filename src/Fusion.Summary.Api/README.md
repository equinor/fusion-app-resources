# Database model

Migrations are by default added to the `Migrations` folder.

## How-to

### Adding migration

1. Open database project in command line
1. Run `dotnet tool restore` if command is not available
1. `dotnet ef migrations add 'Added field ...' --context SummaryDbContext`

### Apply migration on a local environment

1. Run `dotnet ef database update --context SummaryDbContext`