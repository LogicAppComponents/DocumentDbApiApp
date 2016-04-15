using DocumentDBApiApp;
using Microsoft.Azure.Documents;
using System.Collections.Generic;
using System.Web.Http;

namespace DocumentDBApiApp.Controllers
{
    public class CollectionController : ApiController
    {       

        // GET: api/DocumentDB
        public IEnumerable<dynamic> Get(string collection, string fields = "value c", string where = "", string join = "")
        {
            var query = new SqlQuerySpec(
                 "SELECT " + fields + " FROM c " + (string.IsNullOrEmpty(join) ? "" : " JOIN " + join + " ") + (string.IsNullOrEmpty(where) ? "" : " where " +where),
                 new SqlParameterCollection());
            return DocumentDBHelper.GetDocuments(collection, query,null).documents;

        }

        // GETById: api/DocumentDB/5
        public IEnumerable<dynamic> GetById(string collection, string id, string fields = "value c", string where = "", string join = "")
        {
            return this.Get(collection, fields, ("c.id = '" + id + "'"),join);
        }

        // PUT: api/DocumentDB/5
        public void Put(string collection, string id, string document)
        {
            dynamic value = System.Web.Helpers.Json.Decode(document);
            value.id = id;
            DocumentDBHelper.UpsertDocumentAsync(collection, value);
        }

        // POST: api/DocumentDB/collection
        public void PostList(string collection,[FromBody] IEnumerable<dynamic> entites)
        {
            //dynamic value = System.Web.Helpers.Json.Decode(document);

            foreach( var d in entites) {
                
                DocumentDBHelper.UpsertDocumentAsync(collection, d);
            }
            
        }

        // DELETE: api/DocumentDB/5
        public void Delete(string collection, string id)
        {
            var result = DocumentDBHelper.DeleteDocument(id).GetAwaiter().GetResult();
        }
    }
}
