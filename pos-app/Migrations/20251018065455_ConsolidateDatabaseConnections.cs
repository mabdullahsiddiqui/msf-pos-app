using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_app.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateDatabaseConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Users table first
            migrationBuilder.AddColumn<int>(
                name: "ConnectionTimeout",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "DatabaseType",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConnectedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "Users",
                type: "TEXT",
                nullable: true);

            // Migrate data from DatabaseConnections to Users
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET 
                    DatabaseType = (
                        SELECT DatabaseType 
                        FROM DatabaseConnections 
                        WHERE DatabaseConnections.UserId = Users.Id 
                        AND DatabaseConnections.IsActive = 1
                        LIMIT 1
                    ),
                    FilePath = (
                        SELECT FilePath 
                        FROM DatabaseConnections 
                        WHERE DatabaseConnections.UserId = Users.Id 
                        AND DatabaseConnections.IsActive = 1
                        LIMIT 1
                    ),
                    ConnectionTimeout = COALESCE((
                        SELECT ConnectionTimeout 
                        FROM DatabaseConnections 
                        WHERE DatabaseConnections.UserId = Users.Id 
                        AND DatabaseConnections.IsActive = 1
                        LIMIT 1
                    ), 30),
                    LastConnectedAt = (
                        SELECT LastConnectedAt 
                        FROM DatabaseConnections 
                        WHERE DatabaseConnections.UserId = Users.Id 
                        AND DatabaseConnections.IsActive = 1
                        LIMIT 1
                    ),
                    LastError = (
                        SELECT LastError 
                        FROM DatabaseConnections 
                        WHERE DatabaseConnections.UserId = Users.Id 
                        AND DatabaseConnections.IsActive = 1
                        LIMIT 1
                    )
                WHERE EXISTS (
                    SELECT 1 
                    FROM DatabaseConnections 
                    WHERE DatabaseConnections.UserId = Users.Id 
                    AND DatabaseConnections.IsActive = 1
                );
            ");

            // Now drop the DatabaseConnections table
            migrationBuilder.DropTable(
                name: "DatabaseConnections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionTimeout",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DatabaseType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastConnectedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionTimeout = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatabaseName = table.Column<string>(type: "TEXT", nullable: true),
                    DatabaseType = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: true),
                    ServerName = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseConnections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_UserId",
                table: "DatabaseConnections",
                column: "UserId");
        }
    }
}
