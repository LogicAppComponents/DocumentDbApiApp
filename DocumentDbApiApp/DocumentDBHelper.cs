using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
namespace DocumentDbConnector
{
    public class DocumentDBHelper<T>
    {




        public IEnumerable<dynamic> GetByQuery(string collection, string fields = "*", string where = "")
        {
            ConfigurationManager.AppSettings["collection"] = collection;
            /*  IQueryable<dynamic> queryable = Client.CreateDocumentQuery<dynamic>(
                  Collection.SelfLink,
                  new SqlQuerySpec
                  {
                      QueryText = "SELECT "+fields+" FROM "+collection+" c WHERE (c."+where+" = @id)",
                      Parameters = new SqlParameterCollection()
              {
                            new SqlParameter("@id", "??????")
                      }
                  });*/


              var query = new SqlQuerySpec(
                   "SELECT " + fields + " FROM c WHERE (c." + where + " = @id)",
                   new SqlParameterCollection(new SqlParameter[] { new SqlParameter()}));


       
            return DocumentDBHelper<Document>.GetDocuments(query);

            //return DocumentDBRepository<Document>.GetDocuments().Where(d => d.Id == "").Skip(pagesize.Value*pagenumber.Value).Take(pagesize.Value).Select(s => s);

        }

        // private static DocumentClient Client;
        //private static DocumentCollection Collection;
        public static IEnumerable<dynamic> GetDocuments(SqlQuerySpec spec, FeedOptions feedOptions = null)
        {
            return Client.CreateDocumentQuery(Collection.DocumentsLink, spec, feedOptions).AsEnumerable();
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

        //Use the DocumentCollection if it exists, if not create a new Collection
        private static DocumentCollection ReadOrCreateCollection(string databaseLink)
        {
            var col = Client.CreateDocumentCollectionQuery(databaseLink)
                              .Where(c => c.Id == CollectionId)
                              .AsEnumerable()
                              .FirstOrDefault();

            if (col == null)
            {
                var collectionSpec = new DocumentCollection { Id = CollectionId };
                var requestOptions = new RequestOptions { OfferType = "S2" };

                col = Client.CreateDocumentCollectionAsync(databaseLink, collectionSpec, requestOptions).Result;
            }

            return col;
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
       private static string collectionId;
        private static String CollectionId
        {
            get
            {
                /*if (string.IsNullOrEmpty(collection))
                {
                    collectionId = ConfigurationManager.AppSettings["collection"];
                }*/

                return collectionId;
            }
        }

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

        //Use the ReadOrCreateCollection function to get a reference to the collection.
        private static DocumentCollection collection;
        private static DocumentCollection Collection
        {
            get
            {
                if (collection == null)
                {
                    collection = ReadOrCreateCollection(Database.SelfLink);
                }

                return collection;
            }
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
     





    }
}