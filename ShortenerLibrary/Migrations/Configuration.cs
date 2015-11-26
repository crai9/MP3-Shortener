namespace ShortenerLibrary.Migrations
{
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<ShortenerLibrary.Models.SamplesContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(ShortenerLibrary.Models.SamplesContext context)
        {
            // below is what you need to copy into your own class
            context.Samples.AddOrUpdate(s => s.Title,
            new Sample
            {
                Title = "Track 1",
                Artist = "Craig",
                MP3Blob = null,
                SampleMP3Blob = null,
                SampleMP3URL = null,
                DateOfSampleCreation = DateTime.Now

            },
            new Sample
            {
                Title = "Track 2",
                Artist = "Max",
                MP3Blob = null,
                SampleMP3Blob = null,
                SampleMP3URL = null,
                DateOfSampleCreation = DateTime.Now
            }
            );
        }
    }
}