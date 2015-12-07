using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortenerLibrary.Models
{
    //Sample Context that is referenced through the solution in order to query the database via Entity Framework.
    public class SamplesContext : DbContext
    {
        public SamplesContext() : base("SamplesContext")
        {
        }

        public SamplesContext(string connString) : base(connString)
        {
        }

        public System.Data.Entity.DbSet<ShortenerLibrary.Models.Sample> Samples { get; set; }

    }
}
