using ShortenerLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            await ShowMenu();
        }

        private static async Task ShowMenu()
        {
            Console.WriteLine("1.  GET all sample data");
            Console.WriteLine("2.  GET specific sample data");
            Console.WriteLine("3.  POST new sample");
            Console.WriteLine("4.  DELETE a sample");
            Console.WriteLine("5.  PUT data for existing sample");
            Console.WriteLine("6.  PUT blob for existing sample");
            Console.WriteLine("7.  GET blob for existing sample");

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
                        } else
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
                            PutBlob(input);
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
                            GetBlob(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    default:

                        Console.WriteLine("Invalid input integer, try again..");
                        Console.WriteLine();
                        await ShowMenu();
                        break;
                } 
            } else
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

                Console.WriteLine("{0}\t{1}\t${2}\t{3}", sample.SampleID, sample.Title, sample.Artist, sample.DateOfSampleCreation);

            }
            await ReturnToMenu();
        }

        private static async Task Post()
        {
            //Post a new sample
            Random rand = new Random();
            var sample = new Sample() { Title = "Craigs song v" + rand.Next(50), Artist = "Craig", DateOfSampleCreation = DateTime.Now };
            HttpResponseMessage response = await client.PostAsJsonAsync("api/samples", sample);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Sucessfully Posted new Sample to the api");
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
                response = await client.PutAsJsonAsync("api/samples/" + id, sample);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Successfully updated Sample at ID" + id + " via PUT");
                }
            }

            await ReturnToMenu();
        }

        private static void PutBlob(int id)
        {
            //Put blob to container
        }

        private static void GetBlob(int id)
        {
            //Get blob from container
        }

        private static async Task ReturnToMenu()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            await ShowMenu();
        }
    }
}
