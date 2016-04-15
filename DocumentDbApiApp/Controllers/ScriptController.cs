
using DocumentDBApiApp;
using Microsoft.Azure.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace DocumentDBApiApp.Controllers
{
    public class ScriptController : ApiController
    {
        // Executes SP's
        public Task<IEnumerable<dynamic>> PostExecuteStoreProcedure(string collection, string storedprocedure,[FromBody] dynamic parameters)
        {
            return DocumentDBHelper.ExecuteStoredProcedure(collection, storedprocedure, parameters);
        }

        // GETByQuery: api/DocumentDB
        public  IHttpActionResult GetByQuery(string collection, string query = "Select * from collection", int? maxitemcount = null, string continuationToken = null)
        {
            if (query.StartsWith("select", System.StringComparison.CurrentCultureIgnoreCase))
            {
                var queryObject = new SqlQuerySpec(
                     query,
                     new SqlParameterCollection());
                var result = DocumentDBHelper.GetDocuments(collection, queryObject, maxitemcount, continuationToken);
                if(!string.IsNullOrEmpty(result.ContinuationToken))
                    System.Web.HttpContext.Current.Response.AddHeader("ContinuationToken", result.ContinuationToken);
                return Ok(result.documents);
            }            
            return BadRequest("Expression must start with SELECT");
        }

    }
}