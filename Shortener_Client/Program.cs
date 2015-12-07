using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ShortenerLibrary.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
        //Initialise new Http Client.
        static HttpClient client = new HttpClient();

        //Get Azure Active Directory details from the App.config file.
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        private static string shortenerResourceId = ConfigurationManager.AppSettings["todo:ShortenerResourceId"];
        private static AuthenticationContext authContext = null;

        static void Main(string[] args)
        {
            //Run the console application.
            Init().Wait();

        }

        static async Task Init()
        {
            //Print App Name.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("{0}", "Shortener Test Client\n");

            //Establish new Auth Context.
            authContext = new AuthenticationContext(authority, new FileCache());

            //Uncomment the appropriate Base Address for where the Api is running.
            //client.BaseAddress = new Uri("http://localhost:8080/");
            //client.BaseAddress = new Uri("http://mp3s.cloudapp.net:8080/");
            client.BaseAddress = new Uri("https://localhost:44321/");

            //Clear existing headers.
            client.DefaultRequestHeaders.Accept.Clear();
            //Request JSON instead of XML from the API.
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Checking connection to API...");

            //Send a Get request to "/" to see if the API is online.
            try
            {
                HttpResponseMessage response = await client.GetAsync("/");
                Console.WriteLine();
            }
            catch (Exception ex) when (ex is WebException || ex is HttpRequestException)
            {
                //Exit application if API is offline.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not reach the API. \n" + ex.Message + "\n\nExiting Application");
                Console.ReadKey();
                Environment.Exit(0);
            }

            await ShowMenu();

        }

        private static async Task ShowMenu()
        {
            //Display Menu to the user.
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("1.  GET all sample data");
            Console.WriteLine("2.  GET specific sample data");
            Console.WriteLine("3.  POST new sample");
            Console.WriteLine("4.  DELETE a sample");
            Console.WriteLine("5.  PUT information for existing sample");
            Console.WriteLine("6.  PUT blob data for existing sample");
            Console.WriteLine("7.  GET blob data for existing sample");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("8.  Clear the token cache (logout)");
            Console.WriteLine("0.  Exit the application.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nSelect an option by entering a number..");
            Console.WriteLine();

            //Get user's choice
            int input = 0;
            bool fine = int.TryParse(Console.ReadLine(), out input);

            if (fine)
            {
                bool valid;
                Console.WriteLine();
                //Choose method to run in response to the user's input
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
                        //Get a specific sample
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
                        //Post new sample.
                        Console.WriteLine("Posting hard coded data.");
                        Console.WriteLine();
                        await Post();

                        break;

                    case 4:
                        //Delete a sample with an ID that the user specifies.
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
                        //Put data to the api and overwrite existing data.
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
                        //Put an MP3's data to the data api
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
                        //Download data from a blob.
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

                    case 8: ClearCache();

                        await ReturnToMenu();

                        break;

                    default:
                        //handle invalid numeric options.
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid input integer, try again..");
                        Console.WriteLine();
                        await ShowMenu();
                        break;
                }
            }
            else
            {
                //Handle invalid options.
                Console.WriteLine("Please enter an integer");
                Console.WriteLine();

                await ShowMenu();
            }

        }

        private static async Task GetAll()
        {
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            } else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Get all samples
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples");
            if (response.IsSuccessStatusCode)
            {
                //Convert the JSON to Sample objects.
                IEnumerable<Sample> samples = await response.Content.ReadAsAsync<IEnumerable<Sample>>();
                //Display the Sample data
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
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Get a sample by id
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples/" + id);
            if (response.IsSuccessStatusCode)
            {
                //Convert Json to Sample Object.
                Sample sample = await response.Content.ReadAsAsync<Sample>();

                //Display Sample data on console.
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
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

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
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Send a http delete to the Api with the ID to delete
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
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Overwrite information in an existing record by ID
            HttpResponseMessage response;
            response = await client.GetAsync("api/samples/" + id);
            if (response.IsSuccessStatusCode)
            {
                Sample sample = await response.Content.ReadAsAsync<Sample>();

                //Update record by prepending 'New' to its Title
                sample.Title = "New " + sample.Title;
                //Reset other attributes to null.
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
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Put blob to container over http.

            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\Upload\Largo.mp3");

            using (var stream = File.OpenRead(path))
            {
                //Write file stream to the API.
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

        public static async Task<AuthenticationResult> getAuthResult()
        {
            AuthenticationResult result = null;
            // first, try to get a token silently
            try
            {
                result = authContext.AcquireTokenSilent(shortenerResourceId, clientId);
            }
            catch (AdalException ex)
            {
                // There is no token in the cache; prompt the user to sign-in.
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    UserCredential uc = TextualPrompt();
                    // if you want to use Windows integrated auth, comment the line above and uncomment the one below
                    // UserCredential uc = new UserCredential();
                    try
                    {
                        result = authContext.AcquireToken(shortenerResourceId, clientId, uc);
                        await ShowName();
                    }
                    catch (Exception ee)
                    {
                        ShowError(ee);
                        return null;
                    }
                }
                else
                {
                    // An unexpected error occurred.
                    ShowError(ex);
                    return null;
                }
            }
            return result;
        }

        private static async Task ShowName()
        {
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }
            //Get the user's name from the API
            HttpResponseMessage response;
            response = await client.GetAsync("api/name");
            if (response.IsSuccessStatusCode)
            {
                //Display greeting to the user.
                String name = await response.Content.ReadAsStringAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Welcome " + name + "!");

            }

        }

        private static async Task GetBlob(int id)
        {
            //try to authenticate the user using ad
            AuthenticationResult result = await getAuthResult();

            if (result == null)
            {
                await ReturnToMenu();
            }
            else
            {
                //Send token in request header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }

            //Get blob from container over http

            string path = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\Download\Downloaded-File-" + DateTime.Now.Ticks + ".mp3");

            HttpResponseMessage response = await client.GetAsync("api/data/" + id);

            if (response.IsSuccessStatusCode) {

                //Save stream to a file.
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

        static string ReadPasswordFromConsole()
        {
            //Hide password as it is being typed.
            string password = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            return password;
        }

        static void ShowError(Exception ex)
        {
            //Display an error the the user.
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An unexpected error occurred.");
            string message = ex.Message;
            if (ex.InnerException != null)
            {
                message += Environment.NewLine + "Inner Exception : " + ex.InnerException.Message;
            }
            Console.WriteLine("Message: {0}", message);
        }

        static UserCredential TextualPrompt()
        {
            //Get user's login details.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("There is no token in the cache or you are not connected to your domain.");
            Console.WriteLine("Please enter username and password to sign in.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("User>");
            string user = Console.ReadLine();
            Console.WriteLine("Password>");
            string password = ReadPasswordFromConsole();
            Console.WriteLine("");
            return new UserCredential(user, password);
        }

        private static async Task ReturnToMenu()
        {
            //Returns the user to the Menu method.
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
            Console.WriteLine();
            await ShowMenu();
        }

        static void ClearCache()
        {
            //Deletes the Auth token.
            authContext.TokenCache.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Token cache cleared.");
        }
    }
}
