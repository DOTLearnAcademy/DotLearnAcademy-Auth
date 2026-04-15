using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotLearn.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarS3Key",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "OAuthSubject",
                table: "Users",
                newName: "ProfileImageUrl");

            migrationBuilder.RenameColumn(
                name: "OAuthProvider",
                table: "Users",
                newName: "GoogleSubjectId");

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ProfileImageUrl",
                table: "Users",
                newName: "OAuthSubject");

            migrationBuilder.RenameColumn(
                name: "GoogleSubjectId",
                table: "Users",
                newName: "OAuthProvider");

            migrationBuilder.AddColumn<string>(
                name: "AvatarS3Key",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
