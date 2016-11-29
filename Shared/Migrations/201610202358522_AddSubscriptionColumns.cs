namespace CogsMinimizer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSubscriptionColumns : DbMigration
    {
        public override void Up()
        {

            AddColumn("dbo.Subscriptions", "ExpirationUnclaimedIntervalInDays", c => c.Int(nullable: false));
            AddColumn("dbo.Subscriptions", "ReserveIntervalInDays", c => c.Int(nullable: false));
            AddColumn("dbo.Subscriptions", "DeleteIntervalInDays", c => c.Int(nullable: false));
            AddColumn("dbo.Subscriptions", "SendEmailToCoadmins", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Subscriptions", "SendEmailToCoadmins");
            DropColumn("dbo.Subscriptions", "ReserveIntervalInDays");
            DropColumn("dbo.Subscriptions", "DeleteIntervalInDays");
            DropColumn("dbo.Subscriptions", "ExpirationUnclaimedIntervalInDays");
        }
    }
}
