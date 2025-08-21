using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Summary.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixOpenRequestsWorkloadValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            -- Check if the column exists before trying to update it
            IF EXISTS (
                SELECT 1 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'WeeklySummaryReports' 
                AND COLUMN_NAME = 'OpenRequestsWorkload'
            )
            BEGIN
                PRINT 'OpenRequestsWorkload column found - updating values';
                UPDATE WeeklySummaryReports 
                SET OpenRequestsWorkload = 'N/A';
                
                PRINT CONCAT('Updated ', @@ROWCOUNT, ' rows to N/A');
            END
            ELSE
            BEGIN
                PRINT 'OpenRequestsWorkload column does not exist yet - skipping update';
            END
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
