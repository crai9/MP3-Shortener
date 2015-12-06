﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ShortenerLibrary.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Shortener_ApiWebRole.Controllers
{
    [Authorize]
    public class SamplesController : ApiController
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

        // GET: api/Samples
        
        public IQueryable<Sample>  GetSamples()
        {

            return db.Samples;

        }

        // GET: api/Samples/5
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(int id)
        {
            Sample sample = db.Samples.Find(id);
            if (sample == null)
            {
                return NotFound();
            }

            return Ok(sample);
        }

        // PUT: api/Samples/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(int id, Sample sample)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != sample.SampleID)
            {
                return BadRequest();
            }

            //Check if there was a previous 
            var oldSample = db.Samples.Find(id);

            if (GetSoundsContainer().GetBlockBlobReference(oldSample.SampleMP3Blob).Exists())
            {
                CloudBlockBlob blob = GetSoundsContainer().GetBlockBlobReference(oldSample.SampleMP3Blob);
                blob.Delete();
            }

            db.Entry(oldSample).State = EntityState.Detached;

            db.Entry(sample).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SampleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Samples
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Samples.Add(sample);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = sample.SampleID }, sample);
        }

        // DELETE: api/Samples/5
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(int id)
        {
            Sample sample = db.Samples.Find(id);
            if (sample == null)
            {
                return NotFound();
            }
            if (sample.SampleMP3Blob != null)
            {
                if (GetSoundsContainer().GetBlockBlobReference(sample.SampleMP3Blob).Exists())
                {
                    CloudBlockBlob blob = GetSoundsContainer().GetBlockBlobReference(sample.SampleMP3Blob);
                    blob.Delete();
                }
            }


            db.Samples.Remove(sample);
            db.SaveChanges();

            return Ok(sample);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool SampleExists(int id)
        {
            return db.Samples.Count(e => e.SampleID == id) > 0;
        }
    }
}