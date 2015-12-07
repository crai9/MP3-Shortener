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
using ShortenerLibrary.Models;
using Microsoft.WindowsAzure;

namespace Shortener_WorkerRole
{
    //Worker Role that runs in Azure, recieves messages from the Web Roles to know when to perform an action on an MP3. 
    public class WorkerRole : RoleEntryPoint
    {
        //Define classes that will be used through the scope of the Class.
        private CloudQueue soundQueue;
        private CloudBlobContainer soundBlobContainer;
        private String fullInPath;
        private String fullOutPath;
        private String fileTitle;
        Stopwatch stopWatch = new Stopwatch();
        

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

        //Call the FFMpeg.exe via a process to Shorten it to a sample.
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

                Log("It worked!");

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

            //Log("Exe located at: " + GetExePath());
            Log("Creating sounds blob container");

            //Get reference to the Azure Container.
            var blobClient = storageAccount.CreateCloudBlobClient();
            soundBlobContainer = blobClient.GetContainerReference("sounds");

            //Create the Container if it doesn't already exits.
            if (soundBlobContainer.CreateIfNotExists())
            {
                soundBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }

            //Get a reference to the Azure Cloud Queue for use later, or create it if it doesn't exist.
            Log("Creating sound queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            soundQueue = queueClient.GetQueueReference("soundqueue");
            soundQueue.CreateIfNotExists();

            Log("Storage initialized");
            //Log("localStorage path: " + GetLocalStoragePath());
            return base.OnStart();
        }

        public override void Run()
        {
            Log("Shortener_WorkerRole is running");

            CloudQueueMessage msg = null;
            
            //Run an infinite loop to check for new Queue messages.
            while (true)
            {

                try
                {
                    //poll for new message
                    msg = this.soundQueue.GetMessage();
                    if (msg != null)
                    {
                        //Check here for file name or Sample ID.

                        int id;
                        if (Int32.TryParse(msg.AsString, out id))
                        {
                            //Is a Sample ID, message came from ApiWebRole.
                            //Process the message.
                            ProcessQueueMessageFromApi(msg, id);
                        } else
                        {
                            //Is a file name, message came from WebRole.
                            //Process the message.
                            ProcessQueueMessage(msg);
                        }


                        
                    }
                    else
                    {
                        //Wait one second before checking for new queue messages.
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (StorageException e)
                {
                    Trace.TraceError("Message creating a Storage Exception: '{0}'", e.Message);
                    //remove message from queue if it fails more than five times
                    if (msg != null && msg.DequeueCount > 5)
                    {
                        this.soundQueue.DeleteMessage(msg);
                        Trace.TraceError("Deleting poison queue item: '{0}'", msg.AsString);
                    }
                    //Wait 5 seconds before checking for new messages after an Exception occured.
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private void ProcessQueueMessageFromApi(CloudQueueMessage msg, int id)
        {
            //Get connection to the DB via the Samples Context.
            var dbConnString = CloudConfigurationManager.GetSetting("ShortenerDbConnectionString");
            SamplesContext db = new SamplesContext(dbConnString);

            //Find the record with the ID that was taken from the Message.
            var sample = db.Samples.Find(id);

            Log("ID from queue is " + id);
            Log("CloudQueueMessage is " + msg.AsString);

            Log("File name from DB for ID " + id + " is: " + sample.Title);

            //Store the Blob path as a local variable
            string path = sample.MP3Blob;

            //get input blob
            CloudBlockBlob inputBlob = soundBlobContainer.GetBlockBlobReference(path);

            //make folder for blob to be downloaded into
            string folder = path.Split('\\')[0];
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"\" + folder);

            //download file to local storage
            Log("Downloading blob to local storage...");
            soundBlobContainer.GetBlockBlobReference(path).DownloadToFile(GetLocalStoragePath() + path, FileMode.Create);
            Log("Done downloading");

            //get file's current location
            fullInPath = GetLocalStoragePath() + path;

            //new file name
            string soundName = Path.GetFileNameWithoutExtension(inputBlob.Name) + "cropped.mp3";
            Log("New file name: " + soundName);

            //get and make directory for file output
            fullOutPath = GetLocalStoragePath() + @"out\" + soundName;
            CloudBlockBlob outputBlob = this.soundBlobContainer.GetBlockBlobReference(@"out\" + soundName);
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"out\");

            //shorten the sound to 10s
            Log("Shortening MP3 to 10s.");
            stopWatch.Start();

            CropSound(10);

            stopWatch.Stop();
            Log("Took " + stopWatch.ElapsedMilliseconds + " ms to shorten mp3.");
            stopWatch.Reset();

            //set content type to mp3
            outputBlob.Properties.ContentType = "audio/mpeg3";

            //set id3 tags
            Log("Setting ID3 tags.");
            TagLib.File tagFile = TagLib.File.Create(fullOutPath);


            tagFile.Tag.Comment = "Shortened on WorkerRole Instance " + GetInstanceIndex();
            tagFile.Tag.Conductor = "Craig";
            //Check that title tag isn't null
            fileTitle = tagFile.Tag.Title ?? "File has no original Title Tag";
            tagFile.Save();

            LogMP3Metadata(tagFile);


            //upload blob  from local storage to container
            Log("Returning mp3 to the blob container.");
            using (var fileStream = File.OpenRead(fullOutPath))
            {
                outputBlob.UploadFromStream(fileStream);
            }

            //Add metadata to blob
            Log("Adding metadata to the blob.");
            outputBlob.FetchAttributes();
            outputBlob.Metadata["Title"] = fileTitle;
            outputBlob.Metadata["InstanceNo"] = GetInstanceIndex();
            outputBlob.SetMetadata();

            //Add the SampleMP3Date to the DB record.
            sample.SampleMP3Blob = @"out\" + soundName;
            sample.DateOfSampleCreation = DateTime.Now;

            //Save changes made to the record.
            db.SaveChanges();

            //Print blob metadata to console
            Log("Blob's metadata: ");
            foreach (var item in outputBlob.Metadata)
            {
                Log("   " + item.Key + ": " + item.Value);
            }

            //remove message from queue
            Log("Removing message from the queue.");
            soundQueue.DeleteMessage(msg);

            //remove initial blob
            Log("Deleting the input blob.");
            inputBlob.Delete();

            //remove files from local storage
            Log("Deleting files from local storage.");
            File.Delete(fullInPath);
            File.Delete(fullOutPath);
        }

        private void ProcessQueueMessage(CloudQueueMessage msg)
        {
            //get file's path from message queue
            string path = msg.AsString;

            //get input blob
            CloudBlockBlob inputBlob = soundBlobContainer.GetBlockBlobReference(path);

            //make folder for blob to be downloaded into
            string folder = path.Split('\\')[0];
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"\" + folder);

            //download file to local storage
            Log("Downloading blob to local storage...");
            soundBlobContainer.GetBlockBlobReference(path).DownloadToFile(GetLocalStoragePath() + path, FileMode.Create);
            Log("Done downloading");

            //get file's current location
            fullInPath = GetLocalStoragePath() + path;

            //new file name
            string soundName = Path.GetFileNameWithoutExtension(inputBlob.Name) + "cropped.mp3";
            Log("New file name: " + soundName);

            //get and make directory for file output
            fullOutPath = GetLocalStoragePath() + @"out\" + soundName;
            CloudBlockBlob outputBlob = this.soundBlobContainer.GetBlockBlobReference(@"out\" + soundName);
            System.IO.Directory.CreateDirectory(GetLocalStoragePath() + @"out\");

            //shorten the sound to 10s
            Log("Shortening MP3 to 10s.");
            stopWatch.Start();

            CropSound(10);

            stopWatch.Stop();
            Log("Took " + stopWatch.ElapsedMilliseconds + " ms to shorten mp3.");
            stopWatch.Reset();

            //set content type to mp3
            outputBlob.Properties.ContentType = "audio/mpeg3";

            //set id3 tags
            Log("Setting ID3 tags.");
            TagLib.File tagFile = TagLib.File.Create(fullOutPath);


            tagFile.Tag.Comment = "Shortened on WorkerRole Instance " + GetInstanceIndex();
            tagFile.Tag.Conductor = "Craig";
            //Check that title tag isn't null
            fileTitle = tagFile.Tag.Title ?? "File has no original Title Tag";
            tagFile.Save();

            LogMP3Metadata(tagFile);


            //upload blob  from local storage to container
            Log("Returning mp3 to the blob container.");
            using (var fileStream = File.OpenRead(fullOutPath))
            {
                outputBlob.UploadFromStream(fileStream);
            }

            //Add metadata to blob
            Log("Adding metadata to the blob.");
            outputBlob.FetchAttributes();
            outputBlob.Metadata["Title"] = fileTitle;
            outputBlob.Metadata["InstanceNo"] = GetInstanceIndex();
            outputBlob.SetMetadata();

            //Print blob metadata to console
            Log("Blob's metadata: ");
            foreach (var item in outputBlob.Metadata)
            {
                Log("   " + item.Key + ": " + item.Value);
            }

            //remove message from queue
            Log("Removing message from the queue.");
            soundQueue.DeleteMessage(msg);

            //remove initial blob
            Log("Deleting the input blob.");
            inputBlob.Delete();

            //remove files from local storage
            Log("Deleting files from local storage.");
            File.Delete(fullInPath);
            File.Delete(fullOutPath);
        }

        public override void OnStop()
        {
            //Code to run on a graceful stop of the WebRole
            Log("Shortener_WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Log("Shortener_WorkerRole has stopped");
        }

        //Log data that is taken from the File's Metadata.
        protected void LogMP3Metadata(TagLib.File file)
        {
            
            Log("File's metadata:");

            Log("  Title: " + file.Tag.Title);
            var artist = (file.Tag.AlbumArtists.Length > 0) ? file.Tag.AlbumArtists[0] : "";
            Log("  Artist: " + artist);
            Log("  Album: " + file.Tag.Album);
            Log("  Year: " + file.Tag.Year);
            var genre = (file.Tag.Genres.Length > 0) ? file.Tag.Genres[0] : "";
            Log("  Genre: " + genre);
            Log("  Comment: " + file.Tag.Comment);

        }

        //Short-hand method to write to Azure Compute Emulator's console.
        protected void Log(String msg)
        {
            Trace.TraceInformation(msg);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            //Run tasks asynchronously 
            while (!cancellationToken.IsCancellationRequested)
            {
                Log("Working");
                await Task.Delay(1000);
            }
        }
    }
}
