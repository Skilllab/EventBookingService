using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTraceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trace_parent",
                schema: "Booking",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_state",
                schema: "Booking",
                table: "OutboxMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trace_parent",
                schema: "Booking",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "trace_state",
                schema: "Booking",
                table: "OutboxMessages");
        }
    }
}
