using ShortenerLibrary.Migrations;
using System.Data.Entity.Migrations;

namespace ShortenerLibrary.Migrations
{
    public static class InitialiseSamples
    {
        public static void go()
        {
            var configuration = new Configuration();
            var migrator = new DbMigrator(configuration);
            migrator.Update();
        }
    }
}

// This class is only needed when you come to deploy the service in Azure to create an initial populated database
// It goes in the migrations folder
// call go() from end of the Global.asax.cs code
// COMMENT THE CALL TO go() OUT WHEN YOU RUN IT LOCALLY!
