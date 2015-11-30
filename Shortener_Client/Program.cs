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

            Console.WriteLine("\nSelect an option by entering a number..");

            int input = 0;
            bool fine = int.TryParse(Console.ReadLine(), out input);

            if (fine)
            {
                bool valid;
                switch (input)
                {
                    case 0:

                        //Close application
                        Environment.Exit(0);

                        break;

                    case 1:

                        //GET all samples
                        Console.WriteLine("Getting data on all samples.");
                        await GetAll();

                        break;

                    case 2:

                        Console.WriteLine("Enter the ID of the Sample you want info on");

                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            GetOne(input);
                        } else
                        {
                            await ShowMenu();
                        }
                        

                        break;

                    case 3:
                        Console.WriteLine("Posting hard coded data.");
                        Post();

                        break;

                    case 4:

                        Console.WriteLine("Enter the ID of the Sample you want to delete");
                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            Delete(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    case 5:

                        Console.WriteLine("Enter the ID of the sample you want override");
                        input = 0;
                        valid = int.TryParse(Console.ReadLine(), out input);
                        if (valid)
                        {
                            Put(input);
                        }
                        else
                        {
                            await ShowMenu();
                        }

                        break;

                    case 6:

                        Console.WriteLine("Enter the ID of the blob you want to put to");
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
                        await ShowMenu();
                        break;
                } 
            } else
            {
                Console.WriteLine("Please enter an integer");
                await ShowMenu();
            }

        }



        private static async Task GetAll()
        {
            //Get all samples
            HttpResponseMessage response;
            response = await client.GetAsync("api/Samples");
            if (response.IsSuccessStatusCode)
            {
                IEnumerable<Sample> samples = await response.Content.ReadAsAsync<IEnumerable<Sample>>();
                Console.WriteLine("Samples:");
                foreach (var sample in samples)
                {
                    Console.WriteLine("{0}\t{1}\t${2}\t{3}", sample.SampleID, sample.Title, sample.Artist, sample.DateOfSampleCreation);
                }
            }
            await ReturnToMenu();
        }

        private static void GetOne(int id)
        {
            //Get a sample by id
        }

        private static void Post()
        {
            //Post a new sample
        }

        private static void Delete(int id)
        {
            //Delete a sample
        }

        private static void Put(int id)
        {
            //Get information to an existing record
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
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            await ShowMenu();
        }
    }
}
