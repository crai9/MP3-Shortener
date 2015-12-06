using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using ShortenerLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

using System.Collections.Concurrent;
using System.Security.Claims;

namespace Shortener_ApiWebRole.Controllers
{
    [Authorize]
    public class DataController : ApiController
    {

        private SamplesContext db = new SamplesContext();
        private static CloudBlobClient blobClient;
        private static CloudQueueClient queueStorage;

        private static bool created = false;
        private static object @lock = new Object();

        private void MakeContainerAndQueue()
        {
            if (created)
                return;
            lock (@lock)
            {
                if (created)
                {
                    return;
                }

                try
                {

                    var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

                    blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference("sounds");

                    container.CreateIfNotExists();

                    var permissions = container.GetPermissions();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    container.SetPermissions(permissions);

                    queueStorage = storageAccount.CreateCloudQueueClient();

                    CloudQueue queue = queueStorage.GetQueueReference("soundqueue");

                    queue.CreateIfNotExists();
                }
                catch (WebException)
                {

                    throw new WebException("The Windows Azure storage services cannot be contacted " +
                         "via the current account configuration or the local development storage emulator is not running. ");
                }

                created = true;
            }
        }

        private CloudBlobContainer GetSoundsContainer()
        {
            //returns the container where the sound files are stored
            MakeContainerAndQueue();
            return blobClient.GetContainerReference("sounds");
        }

        private CloudQueue GetSoundQueue()
        {
            //returns the CloudQueue where sound paths are stored temp
            MakeContainerAndQueue();
            return queueStorage.GetQueueReference("soundqueue");
        }

        // GET: api/Data/5
        public HttpResponseMessage Get(int id)
        {

            HttpResponseMessage res = new HttpResponseMessage(HttpStatusCode.OK);

            if (db.Samples.Any(o => o.SampleID == id))
            {
                var sample = db.Samples.Find(id);

                if (sample.SampleMP3Blob == null || sample.SampleMP3Blob == "")
                {
                    res.StatusCode = HttpStatusCode.NotFound;
                    res.Content = new StringContent("No content found for that sample");

                    return res;
                }

                CloudBlockBlob blob = GetSoundsContainer().GetBlockBlobReference(sample.SampleMP3Blob);

                Stream blobStream = blob.OpenRead();

                res.Content = new StreamContent(blobStream);
                res.Content.Headers.ContentLength = blob.Properties.Length;
                res.Content.Headers.ContentType = new
                System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg3");
                res.Content.Headers.ContentDisposition = new
                System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = blob.Name,
                    Size = blob.Properties.Length
                };

                return res;

            }
            else
            {
                //Sample not found
                res.Content = new StringContent("Sample with id " + id + " doesn't exist, so you can't download it");
                res.StatusCode = HttpStatusCode.NotFound;
            }


            //1. Connect to DB, check if sample with that ID exists, if not return 404.
            //2. Set response headers, etc.
            //3. Connect to Azure, get reference to the blob.
            //4. Set message's content.
            //5. return the message.

            db.SaveChanges();
            return res;
        }

        // PUT: api/Data/5
        public HttpResponseMessage Put(int id)
        {
            HttpResponseMessage res = new HttpResponseMessage();

            if (db.Samples.Any(o => o.SampleID == id))
            {
                // Sample is in the DB
                var sample = db.Samples.Find(id);

                res.StatusCode = HttpStatusCode.OK;

                if (sample.SampleMP3Blob != null)
                {
                    //remove old blob first
                    if (GetSoundsContainer().GetBlockBlobReference(sample.SampleMP3Blob).Exists())
                    {
                        CloudBlockBlob oldBlob = GetSoundsContainer().GetBlockBlobReference(sample.SampleMP3Blob);
                        oldBlob.Delete();
                    }
                }

                //Set the sample url, aka the Get method of this controller
                var baseUrl = Request.RequestUri.Host + ":" + (Int32.Parse(Request.RequestUri.GetComponents(UriComponents.StrongPort, UriFormat.SafeUnescaped)));

                String sampleMP3URL = "http://" +  baseUrl.ToString() + "/api/data/" + id;
                sample.SampleMP3URL = sampleMP3URL;

                var request = HttpContext.Current.Request;

                //rename file

                var newName = string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ".mp3");
                

                //set initial folder name to 'in'
                String path = @"in\" + newName;

                //get the blob as a variable
                var blob = GetSoundsContainer().GetBlockBlobReference(path);

                //upload blob
                blob.UploadFromStream(request.InputStream);

                //Store the mp3 blob uri in DB
                sample.MP3Blob = path;

                db.SaveChanges();

                //db.Entry(sample).State = System.Data.Entity.EntityState.Detached;

                //Add sample id to the queue
                GetSoundQueue().AddMessage(new CloudQueueMessage(id.ToString()));


                res.Content = new StringContent("Uploaded data to container via put for id: " + id);

            } else {

                //Sample not found
                res.Content = new StringContent("Sample with id " + id + " doesn't exist, so you can't put that data here.");
                res.StatusCode = HttpStatusCode.NotFound;
            }

            return res;

            //1. Connect to DB, check if sample with that ID exists, if not return 404.
            //2. Get data from the PUT request.
            //3. Check for MP3.
            //4. Upload blob to the container.
            //5. Update DB record.
            //6. Add message to Queue containing the Sample ID.
        }

    }
}
