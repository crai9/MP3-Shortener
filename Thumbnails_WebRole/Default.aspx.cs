using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Linq;
using System.Net;

namespace Thumbnails_WebRole
{
    public partial class _Default : System.Web.UI.Page
    {
        private static CloudBlobClient blobClient;
        private static CloudQueueClient queueStorage;

        private static bool s_createdContainerAndQueue = false;
        private static object s_lock = new Object();

        private void CreateOnceContainerAndQueue()
        {
            if (s_createdContainerAndQueue)
                return;
            lock (s_lock)
            {
                if (s_createdContainerAndQueue)
                {
                    return;
                }

                try
                {
                    // Obtain connection to this application's
                    // Azure Storage Service account. It will be
                    // used to access both blobs and queues. 

                    var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

                    // open this section to see connection 
                    // to blob containers for storing images

                    // Instantiate the logical client object used for
                    // communicating with a blob container. 

                    blobClient = storageAccount.CreateCloudBlobClient();

                    // Associate that logical client object with a physical
                    // blob container. If we knew that the blob container
                    // already existed, this would be all that we needed. 

                    CloudBlobContainer container = blobClient.GetContainerReference("photogallery");

                    // Create the physical blob container underlying the logical
                    // CloudBlobContainer object, if it doesn't already exist. A 
                    // production app will frequently not do this, instead
                    // requiring the initial administrative provisioning 
                    // process to set up blob containers and other storage structures. 

                    container.CreateIfNotExists();

                    // Set the permission on the blob container
                    // to allow anonymous access

                    var permissions = container.GetPermissions();
                    permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                    container.SetPermissions(permissions);

                    // open this section to see connection 
                    // to queues for passing messages to worker role

                    // Create the queue for communicating with
                    // the Worker role, if the queue doesn't currently
                    // exist

                    // Instantiate a client object for communicating
                    // with a message queue

                    queueStorage = storageAccount.CreateCloudQueueClient();

                    // Connect the client object to a specific CloudQueue
                    // logical object in the storage service. If we were
                    // sure that physical queue underlying this logical 
                    // object already existed, this would be all we needed.

                    CloudQueue queue = queueStorage.GetQueueReference("thumbnailmaker");

                    // Create the physical queue underlying the logical
                    // CloudQueue object, if it doesn't already exist. A 
                    // production app will frequently not do this, instead
                    // requiring the initial administrative provisioning 
                    // process to set up queues and other storage structures. 

                    queue.CreateIfNotExists();
                }
                catch (WebException)
                {
                    // display a nice error message if the local development storage tool is not running or if there is 
                    // an error in the account configuration that causes this exception
                    throw new WebException("The Windows Azure storage services cannot be contacted " +
                         "via the current account configuration or the local development storage emulator is not running. ");
                }

                s_createdContainerAndQueue = true;
                System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Good to go..."));
            }
        }

        private CloudBlobContainer GetPhotoGalleryContainer()
        {
            CreateOnceContainerAndQueue();
            return blobClient.GetContainerReference("photogallery");
        }

        private CloudQueue GetThumbnailMakerQueue()
        {
            CreateOnceContainerAndQueue();
            return queueStorage.GetQueueReference("thumbnailmaker");
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
            return "application/octet-stream";
        }

        // User clicked the "Submit" button
        protected void submitButton_Click(object sender, EventArgs e)
        {
            if (upload.HasFile)
            {
                // Get the file name specified by the user. 

                var ext = System.IO.Path.GetExtension(upload.FileName);

                // Add more information to it so as to make it unique
                // within all the files in that blob container

                var name = string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);

                // Upload photo to the cloud. Store it in a new 
                // blob in the specified blob container. 

                // open this section to see the code. 

                // Go to the container, instantiate a new blob
                // with the descriptive name

                String path = "images/" + name;

                var blob = GetPhotoGalleryContainer().GetBlockBlobReference(path);

                // The blob properties object (the label on the bucket)
                // contains an entry for MIME type. Set that property.

                blob.Properties.ContentType = GetMimeType(upload.FileName);
                var fileArray = upload.FileName.Split('.');
                var extenstion = fileArray[fileArray.Length -1];

                if (extenstion != "mp3")
                {
                    Response.Write("You can only upload mp3 files");
                } else
                {
                    // Actually upload the data to the
                    // newly instantiated blob

                    blob.UploadFromStream(upload.FileContent);

                    // Place a message in the queue to tell the worker
                    // role that a new photo blob exists, which will 
                    // cause it to create a thumbnail blob of that photo
                    // for easier display. 

                    // open this section to see the code

                    GetThumbnailMakerQueue().AddMessage(new CloudQueueMessage(System.Text.Encoding.UTF8.GetBytes(path)));

                    System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Enqueued '{0}'", path));

                    System.Threading.Thread.Sleep(3000);
                    Response.Redirect("Default.aspx?uploaded=1");
                }


            }
        }

        // rerun every timer click - set by timer control on aspx page to be every 1000ms
        protected void Page_PreRender(object sender, EventArgs e)
        {
            try
            {
                // Look at blob container that contains the thumbnails
                // generated by the worker role. Perform a query
                // of the its contents and return the list of all of the
                // blobs whose name begins with the string "thumbnails". 
                // It returns an enumerator of their URLs. 
                // Place that enumerator into list view as its data source. 

                ThumbnailDisplayControl.DataSource = from o in GetPhotoGalleryContainer().GetDirectoryReference("thumbnails").ListBlobs()
                                                     select new { Url = o.Uri };

                // Tell the list view to bind to its data source, thereby
                // showing 
                
                ThumbnailDisplayControl.DataBind();
            }
            catch (Exception)
            {
            }
        }
    }
}
