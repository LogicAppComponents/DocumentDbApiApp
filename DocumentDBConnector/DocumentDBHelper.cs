using DocumentDBConnector.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client.TransientFaultHandling;
using Microsoft.Azure.Documents.Client.TransientFaultHandling.Strategies;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Web.Helpers;
using System.Threading;
using System.Net.Http;
using System.Net;
using Microsoft.ServiceBus.Messaging;

namespace DocumentDBConnector
{


    public static class DocumentDBHelper
    {
        public static void Init()
        {
            ReadOrCreateDatabase();

            var col = Client.CreateDocumentCollectionQuery(Database.SelfLink).AsEnumerable();
            foreach (var collection in col)
            {
                collections.Add(collection);
            }


        }
        //Use the Database if it exists, if not create a new Database
        private static Database ReadOrCreateDatabase()
        {
            var db = Client.CreateDatabaseQuery()
                            .Where(d => d.Id == DatabaseId)
                            .AsEnumerable()
                            .FirstOrDefault();

            /*     if (db == null)
                 {
                     db = Client.CreateDatabaseAsync(new Database { Id = DatabaseId }).Result;
                 }*/

            return db;
        }




        public static string dbLink()
        {
            return Database.SelfLink;
        }
        //Expose the "database" value from configuration as a property for internal use
        private static string databaseId;
        private static String DatabaseId
        {
            get
            {
                if (string.IsNullOrEmpty(databaseId))
                {
                    databaseId = ConfigurationManager.AppSettings["database"];
                }

                return databaseId;
            }
        }

        //Expose the "collection" value from configuration as a property for internal use


        //Use the ReadOrCreateDatabase function to get a reference to the database.
        private static Database database;
        private static Database Database
        {
            get
            {
                if (database == null)
                {
                    database = ReadOrCreateDatabase();
                }

                return database;
            }
        }

        //Use the DocumentCollection if it exists, if not create a new Collection
        private static ConcurrentBag<DocumentCollection> collections = new ConcurrentBag<DocumentCollection>();
        private static object lockobject = new object();
        public static DocumentCollection GetCollection(string collectionid)
        {
            lock (lockobject)
            {
                var collection = collections.Where(rr => rr.Id == collectionid).FirstOrDefault();
                if (collection == null)
                {
                    collection = ReadOrCreateCollection(collectionid);
                    collections.Add(collection);
                }
                return collection;
            }

        }

        private static ConcurrentBag<StoredProcedure> procedures = new ConcurrentBag<StoredProcedure>();

        public static StoredProcedure GetProcedure(string procedureid, DocumentCollection collection)
        {
            var procedure = procedures.Where(rr => rr.Id == procedureid).FirstOrDefault();
            if (procedure == null)
            {
                procedure = ReadProcedure(procedureid, collection);
                procedures.Add(procedure);
            }
            return procedure;

        }

        //This property establishes a new connection to DocumentDB the first time it is used, 
        //and then reuses this instance for the duration of the application avoiding the
        //overhead of instantiating a new instance of DocumentClient with each request
        private static DocumentClient client;
        private static DocumentClient Client
        {
            get
            {
                lock (lockobject)
                {
                    if (client == null)
                    {
                        string endpoint = ConfigurationManager.AppSettings["endpoint"];
                        string authKey = ConfigurationManager.AppSettings["authKey"];

                        Uri endpointUri = new Uri(endpoint);
                        client = new DocumentClient(endpointUri, authKey, new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });

                    }
                }

                return client;
            }
        }

        public static DocumentCollection ReadOrCreateCollection(string collid)
        {
            var col = Client.CreateDocumentCollectionQuery(Database.SelfLink)
                              .Where(c => c.Id == collid)
                              .AsEnumerable()
                              .FirstOrDefault();

            /* if (col == null)
             {
                 var collectionSpec = new DocumentCollection { Id = collid };
                 var requestOptions = new RequestOptions { OfferType = "S2" };


                 col = Client.CreateDocumentCollectionAsync(Database.SelfLink, collectionSpec, requestOptions).Result;
             }*/

            return col;
        }

        public static StoredProcedure ReadProcedure(string procedureid, DocumentCollection collection)
        {
            var procedure = Client.CreateStoredProcedureQuery(collection.SelfLink)
                              .Where(c => c.Id == procedureid)
                              .AsEnumerable()
                              .FirstOrDefault();

            if (procedure == null)
                throw new Exception("Could not find procedure");
            return procedure;
        }


        public static async Task<IEnumerable<dynamic>> ExecuteStoredProcedure(string collectionid, string storedprocedure, params dynamic[] parameters)
        {
            var proc = GetProcedure(storedprocedure, GetCollection(collectionid));

            var executedprocedure = await Client.ExecuteStoredProcedureAsync<String>(proc.SelfLink, parameters);

            var result = JObject.Parse("{ \"list\" :" + executedprocedure + "}");
            return RemoveDocumentDBLocalPropertiesInList(result.First.First);
        }

        private static IEnumerable<dynamic> RemoveDocumentDBLocalPropertiesInList(JToken objectlist)
        {
            foreach (var i in objectlist)
            {
                if (i.HasValues)
                    RemoveDocumentDBLocalProperties((Newtonsoft.Json.Linq.JObject)i);
            }
            return objectlist;
        }
        private static void RemoveDocumentDBLocalProperties(Newtonsoft.Json.Linq.JObject obj)
        {
            obj.Remove("_rid");
            obj.Remove("_self");
            obj.Remove("_etag");
            obj.Remove("_ts");
            obj.Remove("_attachments");
            obj.Remove("_batchguid");
        }
        public class DocumentDBResult
        {
            public IEnumerable<dynamic> documents;
            public string ContinuationToken;

        }


        public static DocumentDBResult GetDocuments(string collectionid, SqlQuerySpec spec, int? maxitemcount, string continuationToken = null)
        {
            FeedOptions feed = null;
            if (maxitemcount.HasValue && maxitemcount.Value > 0 || !String.IsNullOrEmpty(continuationToken))
            {
                feed = new FeedOptions() { MaxItemCount = ((maxitemcount.HasValue && maxitemcount.Value > 0) ? maxitemcount.Value : new int?()), RequestContinuation = (String.IsNullOrEmpty(continuationToken) ? null : continuationToken) };

                var Query = client.CreateDocumentQuery(GetCollection(collectionid).DocumentsLink, spec, feed).AsDocumentQuery();

                //do
                //{
                var result = Query.ExecuteNextAsync().GetAwaiter().GetResult();
                var res = RemoveDocumentDBLocalPropertiesQuery(result.ToList());
                return new DocumentDBResult() { documents = res, ContinuationToken = result.ResponseContinuation };

                //} while (continuationToken == null && maxitemcount == null && Query.HasMoreResults);


            }

            else {
                var response = new DocumentDBResult();
                var Query = client.CreateDocumentQuery(GetCollection(collectionid).DocumentsLink, spec, null).AsDocumentQuery();
                while (Query.HasMoreResults)
                {
                    var result = Query.ExecuteNextAsync().GetAwaiter().GetResult();
                    var res = RemoveDocumentDBLocalPropertiesQuery(result.ToList());
                    if (response.documents == null)
                    {
                        response.documents = res;
                    }
                    else
                    {
                        ((List<dynamic>)response.documents).AddRange(res);
                    }
                }
                return response;
            }

        }

        public static IEnumerable<dynamic> RemoveDocumentDBLocalPropertiesQuery(List<dynamic> query)
        {

            var list = new List<dynamic>();
            foreach (var r in query.ToList())
            {
                var obj = JObject.FromObject(r);
                RemoveDocumentDBLocalProperties(obj);
                list.Add(obj);
            }

            return list;
        }
        private static async Task<V> ExecuteWithRetries<V>(DocumentClient client, Func<Task<V>> function, Guid id)
        {
            TimeSpan sleepTime = TimeSpan.Zero;
            
            while (true)
            {

                try
                {
                    var t = await function();
                    var status = GetStatusObject(id);
                    if (status != null)
                    {
                        status.IncreasRecordsDone();
                    }

                    return t;
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != 429 && (int)de.StatusCode != 503 && (int)de.StatusCode != 410)
                    {
                        var status = GetStatusObject(id);
                        status.IncreaseFailedRecords();
                        status.errorMsg = de.Message;
                        throw;

                    }
                    sleepTime = de.RetryAfter;
                }
                catch (System.Net.Http.HttpRequestException hre)
                {
                    sleepTime = TimeSpan.FromMilliseconds(20);
                }


                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        var status = GetStatusObject(id);
                        status.IncreaseFailedRecords();
                        status.errorMsg = ae.Message;
                        throw;
                    }

                    DocumentClientException de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != 429 && (int)de.StatusCode != 503 && (int)de.StatusCode != 410)
                    {
                        var status = GetStatusObject(id);
                        status.IncreaseFailedRecords();
                        status.errorMsg = de.Message;
                        throw;
                    }

                    sleepTime = de.RetryAfter;
                }
                catch (Exception e)
                {

                    var status = GetStatusObject(id);
                    status.IncreaseFailedRecords();
                    status.errorMsg = e.Message;
                    throw;
                }
                await Task.Delay(sleepTime);
            }            
        }

        private class RequestInformation
        {
            public double RequestCharge;
        }

        private static async Task<RequestInformation> UpsertDocumentSync(string currentcollection, object document, StatusObject status)
        {
            TimeSpan sleepTime = TimeSpan.Zero;            
            for (int i = 0; i < 100; i++)
            {
                try
                {                    
                    var res = await Client.UpsertDocumentAsync(currentcollection, document);                    
                    status.IncreasRecordsDone();
                    return new RequestInformation() { RequestCharge = res.RequestCharge };
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != 429 && (int)de.StatusCode != 503 && (int)de.StatusCode != 410)
                    {
                        status.IncreaseFailedRecords();
                        status.errorMsg = de.Message;
                    }
                    sleepTime = de.RetryAfter;
                }
                catch (System.Net.Http.HttpRequestException hre)
                {
                    sleepTime = TimeSpan.FromMilliseconds(20);
                }


                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        status.IncreaseFailedRecords();
                        status.errorMsg = ae.Message;
                    }

                    DocumentClientException de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != 429 && (int)de.StatusCode != 503 && (int)de.StatusCode != 410)
                    {
                        status.IncreaseFailedRecords();
                        status.errorMsg = de.Message;
                    }

                    sleepTime = de.RetryAfter;
                }
                catch (Exception e)
                {
                    status.IncreaseErrorRecords();
                    status.errorMsg = e.Message;
                    break;
                }

                await Task.Delay(sleepTime);
            }
            return null;
        }


        public static async Task<Document> UpsertDocumentAsync(string collectionid, object document, Guid id)
        {
            var currentcollection = GetCollection(collectionid).DocumentsLink;
           
            return await ExecuteWithRetries(Client, () => Client.UpsertDocumentAsync(currentcollection, document), id);            
        }

        public static async Task<Document> DeleteDocument(string id)
        {
            //Document doc = GetDocument(id);
            return await Client.DeleteDocumentAsync("TODO");

        }


        public static ConcurrentDictionary<Guid, StatusObject> conDictionary = new ConcurrentDictionary<Guid, StatusObject>();


        public static StatusObject CreateStatusObject(int totalrecords)
        {
            StatusObject status = new StatusObject();
            while (!conDictionary.TryAdd(status.id, status)) ;
            status.totalRecords = totalrecords;
            foreach(var s in conDictionary.Where( s => s.Value.lastUpdate.AddHours(12) < DateTime.Now))
            {
                StatusObject obj;
                conDictionary.TryRemove(s.Key, out obj);
            }

            return status;
        }

        public static async Task<StatusObject> ProcessList(string collection, IEnumerable<dynamic> entites, StatusObject value,string callbackurl = null, int timeouttime=30)
        {
            var t = Task.Run<StatusObject>(async () =>
            {

                string errormsg = "";
                try
                {
                    var col = GetCollection(collection);
                    var currentcollection = col.DocumentsLink;
                    foreach (var d in entites)
                    {
                        d._batchguid = value.id;
                        
                        RequestInformation robj = await DocumentDBHelper.UpsertDocumentSync(currentcollection, d, value);
                    }
                }
                catch(Exception ex)
                {
                    errormsg = ex.Message;
                    if (value != null)
                        value.errorMsg = ex.Message;
                }

                if (!string.IsNullOrEmpty(callbackurl))
                {
                    //await all done
                    var exededtime = DateTime.Now.AddMinutes(timeouttime);
                    if (value != null)
                    {
                        while ((value.totalRecords > value.recordsDone + value.errorRecords) && DateTime.Now < exededtime)
                        {
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        value = new StatusObject(errormsg);
                    }
                    var client = new WebClient();
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    
                    string response = client.UploadString(callbackurl, Json.Encode(value));
                }
                return value;
            }
            );
            return await t;
        }
        public static StatusObject GetStatusObject(Guid id)
        {
            StatusObject value = null;
            for (int i = 0; i < 50; i++)
            {
                if (conDictionary.TryGetValue(id, out value))
                {
                    return value;
                }
                Thread.Sleep(10);
            }
            return null;
        }
    }
}