using ShortenerLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Shortener_Client
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {

            Init().Wait();

        }

        static async Task Init()
        {
            Console.WriteLine("{0}", "Shortener Test Client\n");

            client.BaseAddress = new Uri("http://localhost:8080/");
            //client.BaseAddress = new Uri("http://mp3s.cloudapp.net:8080/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Checking connection to API...");

            try
            {
                HttpResponseMessage response = await client.GetAsync("/");
                Console.WriteLine();
            }
            catch (Exception ex) when (ex is WebException || ex is HttpRequestException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not reach the API. \n" + ex.Message + "\n\nExiting Application");
                Console.ReadKey();
                Environment.Exit(0);
            }

            await ShowMenu();

        }

        private static async Task ShowMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("1.  GET all sample data");
            Console.WriteLine("2.  GET specific sample data");
            Console.WriteLine("3.  POST new sample");
            Console.WriteLine("4.  DELETE a sample");
            Console.WriteLine("5.  PUT information for existing sample");
            Console.WriteLine("6.  PUT blob data for existing sample");
            Console.WriteLine("7.  GET blob data for existing sample");

            Console.WriteLine("0.  Exit the application.");
            Console.WriteLine();
            Console.WriteLine("\nSelect an option by entering a number..");
            Console.WriteLine();

            int input = 0;
            bool fine = int.TryParse(Console.ReadLine(), out input);

            if (fine)
            {
                bool valid;
                Console.WriteLine();
                switch (input)
                {
                    case 0:

                        //Close application
                        Environment.Exit(0);

                        break;

                    case 1:

                        //GET all samples
                        Console.WriteLine("Getting data on all samples.");
                        Console.WriteLine();
                        await GetAll();

                        break;

                    case 2:

                        Console.WriteLine("Enter the ID of the Sample you want info on");
                        Console.WriteLine();
                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            await GetOne(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }


                        break;

                    case 3:
                        Console.WriteLine("Posting hard coded data.");
                        Console.WriteLine();
                        await Post();

                        break;

                    case 4:

                        Console.WriteLine("Enter the ID of the Sample you want to delete");
                        Console.WriteLine();

                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            await Delete(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    case 5:

                        Console.WriteLine("Enter the ID of the sample you want overwrite");
                        Console.WriteLine();
                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            await Put(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    case 6:

                        Console.WriteLine("Enter the ID of the blob you want to put to");
                        Console.WriteLine();

                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            await PutBlob(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    case 7:

                        Console.WriteLine("Enter the ID of the blob you want to get");
                        Console.WriteLine();

                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            await GetBlob(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid input integer, try again..");
                        Console.WriteLine();
                        await ShowMenu();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Please enter an integer");
                Console.WriteLine();

                await ShowMenu();
            }

        }

        private static async Task GetAll()
        {
            //Get all samples
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples");
            if (response.IsSuccessStatusCode)
            {
                IEnumerable<Sample> samples = await response.Content.ReadAsAsync<IEnumerable<Sample>>();
                Console.WriteLine("Samples:");
                Console.WriteLine();

                Console.WriteLine("{0}\t{1}\t{2}\t{3}", "ID     ", "Title     ", "Artist     ", "Date     ");
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", "--     ", "-----     ", "------     ", "----     ");

                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var sample in samples)
                {
                    Console.WriteLine("{0}     \t{1}     \t{2}     \t{3}", sample.SampleID, sample.Title, sample.Artist, sample.DateOfSampleCreation);
                }
            }
            await ReturnToMenu();
        }

        private static async Task GetOne(int id)
        {
            //Get a sample by id
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples/" + id);
            if (response.IsSuccessStatusCode)
            {

                Sample sample = await response.Content.ReadAsAsync<Sample>();
                Console.WriteLine("Samples:");

                Console.WriteLine("{0}\t{1}\t{2}\t{3}", "ID     ", "Title     ", "Artist     ", "Date     ");
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", "--     ", "-----     ", "------     ", "----     ");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0}\t{1}\t{2}\t{3}", sample.SampleID, sample.Title, sample.Artist, sample.DateOfSampleCreation);

            }
            await ReturnToMenu();
        }

        private static async Task Post()
        {
            //Post a new sample
            Random rand = new Random();
            var sample = new Sample() { Title = "Song " + rand.Next(50), Artist = "Craig", DateOfSampleCreation = DateTime.Now };
            HttpResponseMessage response = await client.PostAsJsonAsync("api/samples", sample);
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Successfully Posted new Sample to the api");
            }

            await ReturnToMenu();
        }

        private static async Task Delete(int id)
        {
            //Delete a sample
            HttpResponseMessage response;
            response = await client.DeleteAsync("api/samples/" + id);

            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Deleted sample at id: " + id);
            }

            await ReturnToMenu();
        }

        private static async Task Put(int id)
        {
            //Overwrite information in an existing record by ID
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples/" + id);
            if (response.IsSuccessStatusCode)
            {
                Sample sample = await response.Content.ReadAsAsync<Sample>();

                //Update record by prepending 'New'to its Title
                sample.Title = "New " + sample.Title;
                sample.MP3Blob = null;
                sample.SampleMP3Blob = null;
                sample.SampleMP3URL = null;
                response = await client.PutAsJsonAsync("api/samples/" + id, sample);
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Successfully updated Sample at ID " + id + " via PUT");
                }
            }

            await ReturnToMenu();
        }

        private static async Task PutBlob(int id)
        {
            //Put blob to container over http

            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\Upload\Largo.mp3");

            using (var stream = File.OpenRead(path))
            {
                HttpResponseMessage res = await client.PutAsync("api/data/" + id, new StreamContent(stream));
                res.EnsureSuccessStatusCode();

                if (res.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("PUT file successfully to sample id " + id);
                }

            }

            await ReturnToMenu();
        }

        private static async Task GetBlob(int id)
        {
            //Get blob from container over http

            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\Download\Downloaded-File-" + DateTime.Now.Ticks + ".mp3");

            HttpResponseMessage response = await client.GetAsync("api/data/" + id);

            if (response.IsSuccessStatusCode) {

                byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;
                Console.WriteLine("Downloaded {0} bytes", bytes.Length);
                using (FileStream fileStream = new FileStream(path,
                FileMode.Create, FileAccess.Write))

                using (BinaryWriter binaryFileWriter = new BinaryWriter(fileStream))
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        binaryFileWriter.Write(bytes[i]);
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Downloaded mp3.");

            }

            await ReturnToMenu();

        }

        private static async Task ReturnToMenu()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            Console.WriteLine();
            await ShowMenu();
        }
    }
}
