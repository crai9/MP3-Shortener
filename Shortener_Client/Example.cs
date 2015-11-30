using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ProductStoreClient
{
    // Based on: http://www.asp.net/web-api/overview/advanced/calling-a-web-api-from-a-net-client
    // Make sure that Nuget Default Project is set to this project before executing:
    //      Install-Package Microsoft.AspNet.WebApi.Client
    class Example
    {
        static void Go()
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:8080/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP POST
                var gizmo = new Product() { Name = "Widget 3", Price = 100.00, Category = "Store Item" };
                HttpResponseMessage response = await client.PostAsJsonAsync("api/products", gizmo);
                if (response.IsSuccessStatusCode)
                {
                    Uri gizmoUrl = response.Headers.Location;

                    // HTTP GET - get specific product
                    response = await client.GetAsync("api/products/1");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Product at {0}: ", gizmoUrl);
                        Product product = await response.Content.ReadAsAsync<Product>();
                        Console.WriteLine("{0}\t${1}\t{2}", product.Name, product.Price, product.Category);
                    }
                    Console.WriteLine();
                    Console.WriteLine();

                    // HTTP GET - get all products
                    response = await client.GetAsync("api/products/");
                    if (response.IsSuccessStatusCode)
                    {
                        IEnumerable<Product> products = await response.Content.ReadAsAsync<IEnumerable<Product>>();
                        Console.WriteLine("Products:");
                        foreach (var product in products)
                        {
                            Console.WriteLine("{0}\t{1}\t${2}\t{3}", product.ProductId, product.Name, product.Price, product.Category);
                        }
                    }
                    Console.WriteLine("POST complete; URI of new resource is: " + gizmoUrl.AbsoluteUri);
                    Console.WriteLine();

                    Console.ReadLine();

                    // HTTP PUT - test with a browser
                    Console.WriteLine("Test PUT");
                    response = await client.GetAsync(gizmoUrl); // last one POSTed

                    if (response.IsSuccessStatusCode)
                    {
                        Product product = await response.Content.ReadAsAsync<Product>();
                        product.Price = 80;   // Update price                       
                        response = await client.PutAsJsonAsync(gizmoUrl, product);
                    }
                    Console.WriteLine("PUT complete; URI of updated resource is: " + gizmoUrl.AbsoluteUri);
                    Console.WriteLine();

                    Console.ReadLine();

                    // HTTP DELETE  - test with a browser
                    Console.WriteLine("Test DELETE");
                    response = await client.DeleteAsync(gizmoUrl);
                    Console.WriteLine("DELETE complete; URI of deleted resource was: " + gizmoUrl.AbsoluteUri);

                    Console.ReadLine();
                }
            }
        }
    }
}
