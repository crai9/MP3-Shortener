using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortenerLibrary.Models
{
    //Sample Model class that holds data about a sample.
    public class Sample
    {
        public int SampleID { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Artist { get; set; }

        [StringLength(1024)]
        public string MP3Blob { get; set; }

        [StringLength(1024)]
        public string SampleMP3Blob { get; set; }

        [StringLength(1024)]
        public string SampleMP3URL { get; set; }

        public DateTime DateOfSampleCreation { get; set; }
    }
}
