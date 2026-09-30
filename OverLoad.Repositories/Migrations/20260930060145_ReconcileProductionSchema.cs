using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OverLoad.Repositories.Migrations
{
    /// <summary>
    /// Brings the model back in line with production. Five migrations were applied to the production
    /// database from code that has since been lost (AddManagerRoleAndIsLocked,
    /// AddStudentVerificationFields, AddAiLimitsToUser, AddBugReports, AddBugReportAttachmentUrl).
    /// This migration re-declares what they created — six Users columns and the BugReports table —
    /// with the exact production types, defaults and delete rules.
    /// <para>
    /// Every statement is guarded, so it is a no-op on production (where the objects already exist)
    /// and creates them on a fresh database.
    /// </para>
    /// </summary>
    public partial class ReconcileProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'HasSeenStudentRejection') IS NULL
    ALTER TABLE [Users] ADD [HasSeenStudentRejection] bit NOT NULL DEFAULT CAST(0 AS bit);
IF COL_LENGTH('Users', 'StudentCardPath') IS NULL
    ALTER TABLE [Users] ADD [StudentCardPath] nvarchar(500) NULL;
IF COL_LENGTH('Users', 'StudentVerificationStatus') IS NULL
    ALTER TABLE [Users] ADD [StudentVerificationStatus] nvarchar(50) NOT NULL DEFAULT N'NONE';
IF COL_LENGTH('Users', 'IsLocked') IS NULL
    ALTER TABLE [Users] ADD [IsLocked] bit NOT NULL DEFAULT CAST(0 AS bit);
IF COL_LENGTH('Users', 'AiQuestionsAskedToday') IS NULL
    ALTER TABLE [Users] ADD [AiQuestionsAskedToday] int NOT NULL DEFAULT 0;
IF COL_LENGTH('Users', 'LastAiQuestionDate') IS NULL
    ALTER TABLE [Users] ADD [LastAiQuestionDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[BugReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [BugReports] (
        [Id] int NOT NULL IDENTITY(1, 1),
        [UserId] int NOT NULL,
        [CourseId] int NOT NULL,
        [LessonId] int NULL,
        [Title] nvarchar(250) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [InstructorNote] nvarchar(max) NULL,
        [AdminNote] nvarchar(max) NULL,
        [ResolvedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [AttachmentUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_BugReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BugReports_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BugReports_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [Lessons] ([Id]),
        CONSTRAINT [FK_BugReports_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
    CREATE INDEX [IX_BugReports_CourseId] ON [BugReports] ([CourseId]);
    CREATE INDEX [IX_BugReports_LessonId] ON [BugReports] ([LessonId]);
    CREATE INDEX [IX_BugReports_UserId] ON [BugReports] ([UserId]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // On production these objects hold real data created before this migration existed;
            // dropping them here would destroy it.
            throw new NotSupportedException(
                "ReconcileProductionSchema only re-declares objects that already exist in production and cannot be reverted.");
        }
    }
}
