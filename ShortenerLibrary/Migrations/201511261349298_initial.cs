namespace ShortenerLibrary.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Samples",
                c => new
                    {
                        SampleID = c.Int(nullable: false, identity: true),
                        Title = c.String(maxLength: 100),
                        Artist = c.String(maxLength: 100),
                        MP3Blob = c.String(maxLength: 1024),
                        SampleMP3Blob = c.String(maxLength: 1024),
                        SampleMP3URL = c.String(maxLength: 1024),
                        DateOfSampleCreation = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SampleID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Samples");
        }
    }
}
