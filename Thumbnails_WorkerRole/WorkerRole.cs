using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System;

namespace Shortener_WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudQueue soundQueue;
        private CloudBlobContainer soundBlobContainer;
        private String fullInPath;
        private String fullOutPath;
        private String fileTitle;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public static String GetExePath()
        {
           //returns the full path of the executeable file in the worker role
           return Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\ffmpeg.exe");
        }

        public static String GetExeArgs(String inPath, String outPath, int seconds = 10)
        {
            //returns command line arguments
            return "-t "+ seconds + " -i " + inPath + " -acodec copy " + outPath;
        }

        public static String GetLocalStoragePath()
        {
            //returns the full path of the local storage
            LocalResource l = RoleEnvironment.GetLocalResource("LocalSoundStore");
            return string.Format(l.RootPath);
        }

        public static String GetInstanceIndex()
        {
            //returns the instance's index
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            return instanceId.Substring(instanceId.LastIndexOf("_") + 1);
        }

        private bool CropSound(int seconds = 30)
        {
            bool success = false;

            try
            {
                Process proc = new Process();
                //set exe's location
                proc.StartInfo.FileName = GetExePath();
                //set command line args
                proc.StartInfo.Arguments = GetExeArgs(fullInPath, fullOutPath, seconds);
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.ErrorDialog = false;

                //execute code
                proc.Start();
                proc.WaitForExit();
                success = true;

                Trace.TraceInformation("It worked???");

            } catch(Exception e)
            {
                Trace.TraceError(e.StackTrace);
            }



            return success;
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections.
            ServicePointManager.DefaultConnectionLimit = 12;

            // Open storage account using credentials set in role properties and embedded in .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            Trace.TraceInformation("Exe located at: " + GetExePath());
            Trace.TraceInformation("Creating sounds blob container");


            var blobClient = storageAccount.CreateCloudBlobClient();
            soundBlobContainer = blobClient.GetContainerReference("sounds");

            if (soundBlobContainer.CreateIfNotExists())
            {
                soundBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }

            
            Trace.TraceInformation("Creating sound queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            soundQueue = queueClient.GetQueueReference("soundqueue");
            soundQueue.CreateIfNotExists();

            Trace.TraceInformation("Storage initialized");
            Trace.TraceInformation("localStorage path: " + GetLocalStoragePath());
            return base.OnStart();
        }

        public override void Run()
        {
            Trace.TraceInformation("Shortener_WorkerRole is running");

            CloudQueueMessage msg = null;

            while (true)
            {

                try
                {
                    //poll for new message
                    msg = this.soundQueue.GetMessage();
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
                    //remove message from queue if it fails more than five times
                    if (msg != null && msg.DequeueCount > 5)
                    {
                        this.soundQueue.DeleteMessage(msg);
                        Trace.TraceError("Deleting poison queue item: '{0}'", msg.AsString);
                    }
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private void ProcessQueueMessage(CloudQueueMessage msg)
        {
            //get file's path from message queue
            string path = msg.AsString;
            
            Trace.TraceInformation(string.Format("*** WorkerRole: Dequeued '{0}'", path));

            //get input blob
            CloudBlockBlob inputBlob = soundBlobContainer.GetBlockBlobReference(path);

            //make folder for blob to be downloaded into
            string folder = path.Split('\\')[0];
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"\" + folder);

            //download file to local storage
            soundBlobContainer.GetBlockBlobReference(path).DownloadToFile(GetLocalStoragePath() + path, FileMode.Create);

            //
            fullInPath = GetLocalStoragePath() + path;
            Trace.TraceInformation("Full original file path: " + fullInPath);


            //new file name
            string soundName = Path.GetFileNameWithoutExtension(inputBlob.Name) + "cropped.mp3";

            //get and make directory for file output
            fullOutPath = GetLocalStoragePath() + @"out\" + soundName;
            CloudBlockBlob outputBlob = this.soundBlobContainer.GetBlockBlobReference(@"out\" + soundName);
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"out\");

            //shorten the sound to 10s
            CropSound(10);

            //set content type to mp3
            outputBlob.Properties.ContentType = "audio/mpeg3";

            //set id3 tags
            TagLib.File tagFile = TagLib.File.Create(fullOutPath);

            tagFile.Tag.Comment = "Shortened on WorkerRole Instance " + GetInstanceIndex();
            tagFile.Tag.Conductor = "Craig";
            //Check that title tag isn't null
            fileTitle = tagFile.Tag.Title ?? "File has no original Title Tag";
            tagFile.Save();


            //upload blob  from local storage to container
            using (var fileStream = File.OpenRead(fullOutPath))
            {
                outputBlob.UploadFromStream(fileStream);
            }

            //Add metadata to blob
            outputBlob.FetchAttributes();
            outputBlob.Metadata["Title"] = fileTitle;
            outputBlob.Metadata["InstanceNo"] = GetInstanceIndex();
            outputBlob.SetMetadata();

            //remove message from queue
            soundQueue.DeleteMessage(msg);

            //remove initial blob
            inputBlob.Delete();

            //remove files from local storage
            File.Delete(fullInPath);
            File.Delete(fullOutPath);
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Shortener_WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Shortener_WorkerRole has stopped");
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
