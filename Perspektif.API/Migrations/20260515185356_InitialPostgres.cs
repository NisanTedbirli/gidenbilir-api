using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Perspektif.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    FlagEmoji = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nationalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    FlagEmoji = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NationalityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User1Id = table.Column<int>(type: "integer", nullable: false),
                    User2Id = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Users_User1Id",
                        column: x => x.User1Id,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Conversations_Users_User2Id",
                        column: x => x.User2Id,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    BudgetLevel = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Experiences_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Experiences_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Experiences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeletedByUser1 = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeletedByUser2 = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "Experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExperienceLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperienceLikes_Experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "Experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperienceLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExperiencePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CloudinaryUrl = table.Column<string>(type: "text", nullable: false),
                    PublicId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperiencePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperiencePhotos_Experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "Experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, "🍽️", "Yemek & İçecek" },
                    { 2, "🏛️", "Kültür & Tarih" },
                    { 3, "🏨", "Konaklama" },
                    { 4, "🚇", "Ulaşım" },
                    { 5, "🌿", "Doğa & Manzara" },
                    { 6, "🌃", "Gece Hayatı" },
                    { 7, "🛍️", "Alışveriş" },
                    { 8, "🔒", "Güvenlik" },
                    { 9, "🗣️", "Dil & İletişim" },
                    { 10, "💰", "Para & Bütçe" },
                    { 11, "☀️", "Hava & İklim" },
                    { 12, "🎯", "Eğlence & Aktivite" },
                    { 13, "🏥", "Sağlık & Hastane" },
                    { 14, "📋", "Vizyon & Pratik Bilgi" },
                    { 15, "💡", "Diğer" }
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "Code", "FlagEmoji", "Name" },
                values: new object[,]
                {
                    { 1, "TR", "🇹🇷", "Türkiye" },
                    { 2, "DE", "🇩🇪", "Almanya" },
                    { 3, "FR", "🇫🇷", "Fransa" },
                    { 4, "GB", "🇬🇧", "İngiltere" },
                    { 5, "IT", "🇮🇹", "İtalya" },
                    { 6, "ES", "🇪🇸", "İspanya" },
                    { 7, "NL", "🇳🇱", "Hollanda" },
                    { 8, "GR", "🇬🇷", "Yunanistan" },
                    { 9, "PT", "🇵🇹", "Portekiz" },
                    { 10, "AT", "🇦🇹", "Avusturya" },
                    { 11, "CH", "🇨🇭", "İsviçre" },
                    { 12, "BE", "🇧🇪", "Belçika" },
                    { 13, "SE", "🇸🇪", "İsveç" },
                    { 14, "NO", "🇳🇴", "Norveç" },
                    { 15, "DK", "🇩🇰", "Danimarka" },
                    { 16, "PL", "🇵🇱", "Polonya" },
                    { 17, "CZ", "🇨🇿", "Çekya" },
                    { 18, "HU", "🇭🇺", "Macaristan" },
                    { 19, "HR", "🇭🇷", "Hırvatistan" },
                    { 20, "RS", "🇷🇸", "Sırbistan" },
                    { 21, "RO", "🇷🇴", "Romanya" },
                    { 22, "RU", "🇷🇺", "Rusya" },
                    { 23, "FI", "🇫🇮", "Finlandiya" },
                    { 24, "JP", "🇯🇵", "Japonya" },
                    { 25, "KR", "🇰🇷", "Güney Kore" },
                    { 26, "CN", "🇨🇳", "Çin" },
                    { 27, "IN", "🇮🇳", "Hindistan" },
                    { 28, "TH", "🇹🇭", "Tayland" },
                    { 29, "ID", "🇮🇩", "Endonezya" },
                    { 30, "SG", "🇸🇬", "Singapur" },
                    { 31, "MY", "🇲🇾", "Malezya" },
                    { 32, "VN", "🇻🇳", "Vietnam" },
                    { 33, "PH", "🇵🇭", "Filipinler" },
                    { 34, "TW", "🇹🇼", "Tayvan" },
                    { 35, "HK", "🇭🇰", "Hong Kong" },
                    { 36, "NP", "🇳🇵", "Nepal" },
                    { 37, "LK", "🇱🇰", "Sri Lanka" },
                    { 38, "AE", "🇦🇪", "Dubai (BAE)" },
                    { 39, "SA", "🇸🇦", "Suudi Arabistan" },
                    { 40, "QA", "🇶🇦", "Katar" },
                    { 41, "IL", "🇮🇱", "İsrail" },
                    { 42, "EG", "🇪🇬", "Mısır" },
                    { 43, "MA", "🇲🇦", "Fas" },
                    { 44, "ZA", "🇿🇦", "Güney Afrika" },
                    { 45, "KE", "🇰🇪", "Kenya" },
                    { 46, "TN", "🇹🇳", "Tunus" },
                    { 47, "US", "🇺🇸", "ABD" },
                    { 48, "CA", "🇨🇦", "Kanada" },
                    { 49, "MX", "🇲🇽", "Meksika" },
                    { 50, "BR", "🇧🇷", "Brezilya" },
                    { 51, "AR", "🇦🇷", "Arjantin" },
                    { 52, "CO", "🇨🇴", "Kolombiya" },
                    { 53, "PE", "🇵🇪", "Peru" },
                    { 54, "CU", "🇨🇺", "Küba" },
                    { 55, "AU", "🇦🇺", "Avustralya" },
                    { 56, "NZ", "🇳🇿", "Yeni Zelanda" },
                    { 57, "GE", "🇬🇪", "Gürcistan" },
                    { 58, "AZ", "🇦🇿", "Azerbaycan" },
                    { 59, "AL", "🇦🇱", "Arnavutluk" },
                    { 60, "ME", "🇲🇪", "Karadağ" }
                });

            migrationBuilder.InsertData(
                table: "Nationalities",
                columns: new[] { "Id", "Code", "FlagEmoji", "Name" },
                values: new object[,]
                {
                    { 1, "TR", "🇹🇷", "Türk" },
                    { 2, "DE", "🇩🇪", "Alman" },
                    { 3, "FR", "🇫🇷", "Fransız" },
                    { 4, "GB", "🇬🇧", "İngiliz" },
                    { 5, "US", "🇺🇸", "Amerikalı" },
                    { 6, "JP", "🇯🇵", "Japon" },
                    { 7, "IT", "🇮🇹", "İtalyan" },
                    { 8, "ES", "🇪🇸", "İspanyol" },
                    { 9, "RU", "🇷🇺", "Rus" },
                    { 10, "CN", "🇨🇳", "Çinli" },
                    { 11, "NL", "🇳🇱", "Hollandalı" },
                    { 12, "GR", "🇬🇷", "Yunan" },
                    { 13, "PT", "🇵🇹", "Portekizli" },
                    { 14, "AT", "🇦🇹", "Avusturyalı" },
                    { 15, "CH", "🇨🇭", "İsviçreli" },
                    { 16, "BE", "🇧🇪", "Belçikalı" },
                    { 17, "SE", "🇸🇪", "İsveçli" },
                    { 18, "NO", "🇳🇴", "Norveçli" },
                    { 19, "DK", "🇩🇰", "Danimarkalı" },
                    { 20, "PL", "🇵🇱", "Polonyalı" },
                    { 21, "CZ", "🇨🇿", "Çekli" },
                    { 22, "HU", "🇭🇺", "Macar" },
                    { 23, "HR", "🇭🇷", "Hırvat" },
                    { 24, "RO", "🇷🇴", "Rumen" },
                    { 25, "RS", "🇷🇸", "Sırp" },
                    { 26, "FI", "🇫🇮", "Fin" },
                    { 27, "KR", "🇰🇷", "Güney Koreli" },
                    { 28, "IN", "🇮🇳", "Hintli" },
                    { 29, "TW", "🇹🇼", "Tayvanlı" },
                    { 30, "TH", "🇹🇭", "Taylandlı" },
                    { 31, "ID", "🇮🇩", "Endonezyal" },
                    { 32, "SG", "🇸🇬", "Singapurlu" },
                    { 33, "MY", "🇲🇾", "Malezyalı" },
                    { 34, "VN", "🇻🇳", "Vietnamlı" },
                    { 35, "PH", "🇵🇭", "Filipinli" },
                    { 36, "NP", "🇳🇵", "Nepalli" },
                    { 37, "LK", "🇱🇰", "Sri Lankalı" },
                    { 38, "AE", "🇦🇪", "Emiratli" },
                    { 39, "SA", "🇸🇦", "Suudi" },
                    { 40, "QA", "🇶🇦", "Katarlı" },
                    { 41, "IL", "🇮🇱", "İsrailli" },
                    { 42, "EG", "🇪🇬", "Mısırlı" },
                    { 43, "MA", "🇲🇦", "Faslı" },
                    { 44, "ZA", "🇿🇦", "Güney Afrikalı" },
                    { 45, "KE", "🇰🇪", "Kenyalı" },
                    { 46, "TN", "🇹🇳", "Tunuslu" },
                    { 47, "CA", "🇨🇦", "Kanadalı" },
                    { 48, "MX", "🇲🇽", "Meksikalı" },
                    { 49, "BR", "🇧🇷", "Brezilyalı" },
                    { 50, "AR", "🇦🇷", "Arjantinli" },
                    { 51, "CO", "🇨🇴", "Kolombiyalı" },
                    { 52, "PE", "🇵🇪", "Perulu" },
                    { 53, "CU", "🇨🇺", "Kübalı" },
                    { 54, "AU", "🇦🇺", "Avustralyalı" },
                    { 55, "NZ", "🇳🇿", "Yeni Zelandalı" },
                    { 56, "GE", "🇬🇪", "Gürcü" },
                    { 57, "AZ", "🇦🇿", "Azerbaycanlı" },
                    { 58, "AL", "🇦🇱", "Arnavut" },
                    { 59, "ME", "🇲🇪", "Karadağlı" },
                    { 60, "HK", "🇭🇰", "Hong Konglu" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ExperienceId",
                table: "Comments",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_User1_User2",
                table: "Conversations",
                columns: new[] { "User1Id", "User2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_User2Id",
                table: "Conversations",
                column: "User2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceLikes_ExperienceId_UserId",
                table: "ExperienceLikes",
                columns: new[] { "ExperienceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceLikes_UserId",
                table: "ExperienceLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperiencePhotos_ExperienceId",
                table: "ExperiencePhotos",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CategoryId",
                table: "Experiences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_Country_User",
                table: "Experiences",
                columns: new[] { "CountryId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CountryId",
                table: "Experiences",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CreatedAt",
                table: "Experiences",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId",
                table: "Experiences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedAt",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsDeletedByUser1",
                table: "Messages",
                column: "IsDeletedByUser1");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsDeletedByUser2",
                table: "Messages",
                column: "IsDeletedByUser2");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NationalityId",
                table: "Users",
                column: "NationalityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ExperienceLikes");

            migrationBuilder.DropTable(
                name: "ExperiencePhotos");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Nationalities");
        }
    }
}
