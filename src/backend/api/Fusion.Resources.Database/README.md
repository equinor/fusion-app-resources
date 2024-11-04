# Database model

## How-to

### Adding migration

1. Open database project in command line
1. Run `dotnet tool restore` if command is not available
1. `dotnet ef migrations add 'Added field ...'`


Migrations are by default added to the `Migrations` folder. Only one DbContext is present so no need for specification.