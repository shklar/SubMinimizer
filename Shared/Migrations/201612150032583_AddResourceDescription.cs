namespace CogsMinimizer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddResourceDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Resources", "Description", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Resources", "Description");
        }
    }
}
