using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;


namespace DocumentDBApiApp
{

    public static class DocumentDBHelper
    {
        public static void Init()
        {
            ReadOrCreateDatabase();

            var col = Client.CreateDocumentCollectionQuery(Database.SelfLink).AsEnumerable();
            foreach(var collection in col)
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

            if (db == null)
            {
                db = Client.CreateDatabaseAsync(new Database { Id = DatabaseId }).Result;
            }

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

        public static DocumentCollection GetCollection(string collectionid)
        {
            var collection = collections.Where(rr => rr.Id == collectionid).FirstOrDefault();
            if(collection == null)
            {
                collection = ReadOrCreateCollection(collectionid);
                collections.Add(collection);
            }
            return collection;
                 
        }

        private static ConcurrentBag<StoredProcedure> procedures = new ConcurrentBag<StoredProcedure>();

        public static StoredProcedure GetProcedure(string procedureid,DocumentCollection collection)
        {
            var procedure = procedures.Where(rr => rr.Id == procedureid).FirstOrDefault();
            if (procedure == null)
            {
                procedure = ReadProcedure(procedureid,collection);
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
                if (client == null)
                {
                    string endpoint = ConfigurationManager.AppSettings["endpoint"];
                    string authKey = ConfigurationManager.AppSettings["authKey"];
                    Uri endpointUri = new Uri(endpoint);
                    client = new DocumentClient(endpointUri, authKey, new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });
                  
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
            if (col == null)
            {
                var collectionSpec = new DocumentCollection { Id = collid };
                var requestOptions = new RequestOptions { OfferType = "S2" };
               

                col = Client.CreateDocumentCollectionAsync(Database.SelfLink, collectionSpec, requestOptions).Result;
            }

            return col;
        }

        public static StoredProcedure ReadProcedure(string procedureid,DocumentCollection collection)
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

            var executedprocedure = await Client.ExecuteStoredProcedureAsync<String>(proc.SelfLink, parameters );

            var result = JObject.Parse("{ \"list\" :" + executedprocedure + "}");
            return RemoveDocumentDBLocalPropertiesInList(result.First.First);
        }

        private static IEnumerable<dynamic> RemoveDocumentDBLocalPropertiesInList(JToken objectlist)
        {
            foreach (var i in objectlist)
            {
                if(i.HasValues)
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
                feed = new FeedOptions() { MaxItemCount = ((maxitemcount.HasValue && maxitemcount.Value > 0)? maxitemcount.Value  : new int?()), RequestContinuation = (String.IsNullOrEmpty(continuationToken)? null : continuationToken) };
                
                var Query = client.CreateDocumentQuery(GetCollection(collectionid).DocumentsLink, spec, feed).AsDocumentQuery();
                var result = Query.ExecuteNextAsync().GetAwaiter().GetResult();

                var res = RemoveDocumentDBLocalPropertiesQuery(result.ToList());
                return new DocumentDBResult() { documents = res, ContinuationToken = result.ResponseContinuation };
            }
            else {
                var Query = client.CreateDocumentQuery(GetCollection(collectionid).DocumentsLink, spec, null);
                var result = RemoveDocumentDBLocalPropertiesQuery(Query.ToList());
                return new DocumentDBResult() { documents = result, ContinuationToken = null };
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

        public static async Task<Document> UpsertDocumentAsync(string collectionid, object document)
        {
            return await Client.UpsertDocumentAsync(GetCollection(collectionid).DocumentsLink, document);
        }

        public static async Task<Document> DeleteDocument(string id)
        {
            //Document doc = GetDocument(id);
            return await Client.DeleteDocumentAsync("TODO");

        }

    }
}