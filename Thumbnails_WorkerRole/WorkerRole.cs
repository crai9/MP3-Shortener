using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System;

namespace Thumbnails_WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudQueue imagesQueue;
        private CloudBlobContainer imagesBlobContainer;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public String GetExePath()
        {
           return Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\ffmpeg.exe");
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections.
            ServicePointManager.DefaultConnectionLimit = 12;

            // Open storage account using credentials set in role properties and embedded in .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            Trace.TraceInformation("Exe located at: " + GetExePath());
            Trace.TraceInformation("Creating photogallery blob container");
            var blobClient = storageAccount.CreateCloudBlobClient();
            imagesBlobContainer = blobClient.GetContainerReference("photogallery");
            if (imagesBlobContainer.CreateIfNotExists())
            {
                // Enable public access on the newly created "photogallery" container.
                imagesBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }

            Trace.TraceInformation("Creating thumbnails queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            imagesQueue = queueClient.GetQueueReference("thumbnailmaker");
            imagesQueue.CreateIfNotExists();

            Trace.TraceInformation("Storage initialized");
            return base.OnStart();
        }

        public override void Run()
        {
            Trace.TraceInformation("Thumbnails_WorkerRole is running");

            CloudQueueMessage msg = null;

            while (true)
            {
                // 30s wait for demo - look in message queue - comment out as appropriate
                //System.Threading.Thread.Sleep(30000);

                try
                {
                    msg = this.imagesQueue.GetMessage();
                    if (msg != null)
                    {
                        ProcessQueueMessage(msg);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (StorageException e)
                {
                    if (msg != null && msg.DequeueCount > 5)
                    {
                        this.imagesQueue.DeleteMessage(msg);
                        Trace.TraceError("Deleting poison queue item: '{0}'", msg.AsString);
                    }
                    Trace.TraceError("Exception in Thumbnails_WorkerRole: '{0}'", e.Message);
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private void ProcessQueueMessage(CloudQueueMessage msg)
        {
            // The message's contents contains the name of
            // the blob containing the complete photo.

            string path = msg.AsString;
            Trace.TraceInformation(string.Format("*** WorkerRole: Dequeued '{0}'", path));

            // Fetch the blob containing the photo

            CloudBlockBlob inputBlob = imagesBlobContainer.GetBlockBlobReference(path);

            // Create a new blob with the string "thumbnails/" prepended
            // to the photo name. Set its ContentType property.

            string thumbnailName = Path.GetFileNameWithoutExtension(inputBlob.Name) + "changed.mp3";
            CloudBlockBlob outputBlob = this.imagesBlobContainer.GetBlockBlobReference("thumbnails/" + thumbnailName);

            // Notice that ConvertImageToThumbnailJPG() can read/write directly to the blobs using streams

            using (Stream input = inputBlob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                ConvertSound(input, output);
                string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
                var instanceIndex = instanceId.Substring(instanceId.LastIndexOf("_") + 1);
                Trace.WriteLine("Role instance index: " + instanceIndex);
                outputBlob.Properties.ContentType = "image/mpeg3";
            }
            Trace.TraceInformation("Generated thumbnail in blob {0}", thumbnailName);

            // Delete the message from the queue. This isn't
            // done until all the processing has been successfully
            // accomplished, so we know we won't miss one in the case of 
            // an exception. However, in that case, we might execute
            // this code more than once. This is a good example
            // of "at least once" design, which is appropriate 
            // for many cases. 

            imagesQueue.DeleteMessage(msg);
        }

        public void ConvertSound(Stream input, Stream output)
        {
            //logic goes here

            BinaryReader br = new BinaryReader(input);
            byte[] bytes = br.ReadBytes((int)input.Length);

            using (var writer = new BinaryWriter(output))
            {
                writer.Write(bytes);
            }

        }

        // Create thumbnail - the detail is unimportant
        public void ConvertImageToThumbnailJPG(Stream input, Stream output)
        {
            int thumbnailsize = 128;
            int width;
            int height;
            var originalImage = new Bitmap(input);

            if (originalImage.Width > originalImage.Height)
            {
                width = thumbnailsize;
                height = thumbnailsize * originalImage.Height / originalImage.Width;
            }
            else
            {
                height = thumbnailsize;
                width = thumbnailsize * originalImage.Width / originalImage.Height;
            }

            Bitmap thumbnailImage = null;
            try
            {
                thumbnailImage = new Bitmap(width, height);

                using (Graphics graphics = Graphics.FromImage(thumbnailImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            }
            finally
            {
                if (thumbnailImage != null)
                {
                    thumbnailImage.Dispose();
                }
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Thumbnails_WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Thumbnails_WorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
