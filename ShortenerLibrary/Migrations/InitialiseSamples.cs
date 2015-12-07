using ShortenerLibrary.Migrations;
using System.Data.Entity.Migrations;

namespace ShortenerLibrary.Migrations
{
    public static class InitialiseSamples
    {
        //Code called from Global.asax.cs on first run to add records to DB.
        public static void go()
        {
            var configuration = new Configuration();
            var migrator = new DbMigrator(configuration);
            migrator.Update();
        }
    }
}

