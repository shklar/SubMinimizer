namespace CogsMinimizer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddAnalyzeStat : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AnalyzeRecords",
                c => new
                {
                    ID = c.String(nullable: false, maxLength: 128),
                    SubscriptionID = c.String(),
                    SubscriptionName = c.String(),
                    Owner = c.String(),
                    AnalyzeDate = c.DateTime(nullable: false),
                    TotalResources = c.Int(nullable: false),
                    ExpiredResources = c.Int(nullable: false),
                    NewResources = c.Int(nullable: false),
                    DeletedResources = c.Int(nullable: false),
                    FailedDeleteResources = c.Int(nullable: false),
                    ValidResources = c.Int(nullable: false),
                    NotFoundResources = c.Int(nullable: false),
                    NearExpiredResources = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID);

        }

        public override void Down()
        {
            DropTable("dbo.AnalyzeRecords");
        }
    }
}
