using DocumentDBConnector;
using Microsoft.Azure.Documents;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Web.Http;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Http.Headers;
using System.Collections.Specialized;
using System.Linq;
using DocumentDBConnector.Models;
using System.Threading.Tasks;

namespace DocumentDBConnector.Controllers
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
   
        [HttpGet]
        [Route("api/status/{id}")]
        // GETByBatch: api/DocumentDB/5
        public HttpResponseMessage StatusCheck([FromUri] Guid id)
        {
            var status = DocumentDBHelper.GetStatusObject(id);

            if(status == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            if (status.totalRecords > status.recordsDone + status.failedRecords)
            {
                HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);
                string uri = String.Format("{0}://{1}/api/status/{2}", Request.RequestUri.Scheme, Request.RequestUri.Host, status.id);
                responseMessage.Headers.Add("docdbbatchlocation", uri);  //Where the engine will poll to check status
                responseMessage.Headers.Add("docdbReqCharge", status.reqCharge.ToString());
                responseMessage.Headers.Add("docdbErrorMsg", status.errorMsg);
                responseMessage.Headers.Add("docdbFailedRecords", status.failedRecords.ToString());
                responseMessage.Headers.Add("docdbCompletedRecords", status.recordsDone.ToString());
                responseMessage.Headers.Add("docdbTotalRecords", status.totalRecords.ToString());
                responseMessage.Headers.Add("retry-after", "20");   //How many seconds it should wait (20 is default if not included)
                return responseMessage;
            }
            if (status.failedRecords > 0)
            {
                return Request.CreateResponse(HttpStatusCode.Conflict);
            }
            return Request.CreateResponse(HttpStatusCode.OK,status);
        }

        // PUT: api/DocumentDB/5
        public void Put(string collection, string id, string document)
        {
            dynamic value = System.Web.Helpers.Json.Decode(document);
            value.id = id;
            DocumentDBHelper.UpsertDocumentAsync(collection, value, Guid.NewGuid());
        }

        // POST: api/DocumentDB/collection
        public HttpResponseMessage PostList(string collection, [FromBody] IEnumerable<dynamic> entites)
        {
            //dynamic value = System.Web.Helpers.Json.Decode(document);
            var status = DocumentDBHelper.CreateStatusObject(entites.Count());
            Task<StatusObject> task = DocumentDBHelper.ProcessList(collection, entites, status.id);

            HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted);

            string uri = String.Format("{0}://{1}/api/status/{2}", Request.RequestUri.Scheme, Request.RequestUri.Host, status.id);
            responseMessage.Headers.Add("docdbbatchlocation", uri);  //Where the engine will poll to check status
            responseMessage.Headers.Add("docdbReqCharge", status.reqCharge.ToString());  //Where the engine will poll to check status
            responseMessage.Headers.Add("retry-after", "20");   //How many seconds it should wait (20 is default if not included)
            responseMessage.Headers.Add("docdbErrorMsg", status.errorMsg);
            responseMessage.Headers.Add("docdbFailedRecords", status.failedRecords.ToString());
            responseMessage.Headers.Add("docdbCompletedRecords", status.recordsDone.ToString());
            responseMessage.Headers.Add("docdbTotalRecords", status.totalRecords.ToString());
            return responseMessage;

        }

        // DELETE: api/DocumentDB/5
        public void Delete(string collection, string id)
        {
            var result = DocumentDBHelper.DeleteDocument(id).GetAwaiter().GetResult();
        }
    }
   
}
