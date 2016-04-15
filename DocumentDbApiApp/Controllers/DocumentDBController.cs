using Microsoft.Azure.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Microsoft.WindowsAzure.WebSitesExtensions.Models;
using Microsoft.WindowsAzure.Common;
using System.Configuration;
using System.Web.Configuration;
using System.Web;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Partitioning;

namespace DocumentDbConnector.Controllers
{
    public class DocumentDBController : ApiController
    {
   
        // GET: api/DocumentDB
        public IEnumerable<dynamic> Get(string collection, string fields = "*", string where = "")
        {


                var query = new SqlQuerySpec(
                 "SELECT " + fields + " FROM"+collection+"WHERE" + where + " = @id)",
                 new SqlParameterCollection(new SqlParameter[] { new SqlParameter() }));



            return DocumentDBRepository<Document>.GetDocuments(query);

        }

        // GET: api/DocumentDB/5
        public Document Get(string id)
        {
            return DocumentDBRepository<Document>.GetDocuments(d => d.Id == id).FirstOrDefault();
        }

        // POST: api/DocumentDB
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/DocumentDB/5
        public void Put(string id, string document)
        {
            dynamic value = System.Web.Helpers.Json.Decode(document);
            value.id = id;
            DocumentDBRepository<Document>.UpsertDocumentAsync(value);
        }

        // DELETE: api/DocumentDB/5
        public void Delete(string id)
        {            
            var result = DocumentDBRepository<Document>.DeleteDocument(id).GetAwaiter().GetResult();
        }
    }
}
