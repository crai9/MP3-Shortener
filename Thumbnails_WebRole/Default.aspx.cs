using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Linq;
using System.Net;

namespace Shortener_WebRole
{
    public partial class _Default : System.Web.UI.Page
    {
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
                Log(String.Format("*** WebRole: Ready"));
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

        private string GetMimeType(string Filename)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(Filename).ToLowerInvariant();
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                if (key != null)
                {
                    string contentType = key.GetValue("Content Type") as String;
                    if (!String.IsNullOrEmpty(contentType))
                    {
                        return contentType;
                    }
                }
            }
            catch
            {
            }
            return "audio/mpeg3";
        }

        //OnClick of submit button
        protected void submitButton_Click(object sender, EventArgs e)
        {
            if (upload.HasFile)
            {

                var ext = System.IO.Path.GetExtension(upload.FileName);

                //rename file so that it is unique
                var name = string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);

                //set initial folder name to 'in'
                String path = @"in\" + name;

                //get the blob as a variable
                var blob = GetSoundsContainer().GetBlockBlobReference(path);

                //get file's mime type
                blob.Properties.ContentType = GetMimeType(upload.FileName); //doesn't work
                var fileArray = upload.FileName.Split('.');
                var extenstion = fileArray[fileArray.Length -1];

                //see if the file is valid
                if (extenstion != "mp3" & ext != ".mp3")
                {
                    Response.Write("You can only upload mp3 files");
                } else
                {
                    //upload the blob
                    blob.UploadFromStream(upload.FileContent);

                    //notify the worker role that there is a new blob to be processed
                    GetSoundQueue().AddMessage(new CloudQueueMessage(System.Text.Encoding.UTF8.GetBytes(path)));
                    System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Enqueued '{0}'", path));

                    //wait for file to be processed by worker role
                    System.Threading.Thread.Sleep(3000);

                    //redirect to home page 
                    Response.Redirect("/");
                }


            }
        }

        protected String GetTitle(Uri blobURI)
        {
            //returns the title of the file from the Blob metadata
            CloudBlockBlob blob = new CloudBlockBlob(blobURI);
            blob.FetchAttributes();
            return blob.Metadata["Title"];
        }

        protected String GetInstanceIndex(Uri blobURI)
        {
            //returns the Worker role's index from the Blob metadata
            CloudBlockBlob blob = new CloudBlockBlob(blobURI);
            blob.FetchAttributes();
            return blob.Metadata["InstanceNo"];
        }

        protected void Log(String msg)
        {
            System.Diagnostics.Trace.WriteLine(msg);
        }

        //On page load
        protected void Page_PreRender(object sender, EventArgs e)
        {
            try
            { 
                //Query for the URL, Title, and Instance of each blob in the container
                SoundsDisplayControl.DataSource = from o in GetSoundsContainer().GetDirectoryReference("out").ListBlobs()
                                                     select new { Url = o.Uri, Name = GetTitle(o.Uri), Instance = GetInstanceIndex(o.Uri)};
                //Bind data to DisplayControl
                SoundsDisplayControl.DataBind();
            }
            catch (Exception)
            {
            }
        }
    }
}
