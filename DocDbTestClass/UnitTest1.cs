using System;
using DocumentDBConnector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DocumentDBConnector.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;

namespace DocDbTestClass
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void InitTest()
        {
            DocumentDBHelper.Init();
            var collection = DocumentDBHelper.ReadOrCreateCollection("INT002_units_v1");

            Assert.IsNotNull(collection);
        }

        [TestMethod]
        public void CreteMultipleStatusObjects()
        {
            var unitstatus = DocumentDBHelper.CreateStatusObject(500);
             unitstatus = DocumentDBHelper.CreateStatusObject(11);
             unitstatus = DocumentDBHelper.CreateStatusObject(12);
             unitstatus = DocumentDBHelper.CreateStatusObject(13);
             unitstatus = DocumentDBHelper.CreateStatusObject(155);
             unitstatus = DocumentDBHelper.CreateStatusObject(16);

        }


        [TestMethod]
        public void PostListSingleTest()
        {
            //ett record
            var currdir = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
            var testdir = currdir.Parent.Parent.GetDirectories().Where(d => d.Name == "TestFiles").First();

            string units = System.IO.File.ReadAllText(testdir.FullName + "\\2_units.json");
            var unitsCol = "INT002_units_v1";
            var unitlist = Newtonsoft.Json.JsonConvert.DeserializeObject(units) as IEnumerable<dynamic>;
            var unitstatus = DocumentDBHelper.CreateStatusObject(unitlist.Count());

            Task<StatusObject> taskunits = DocumentDBHelper.ProcessList(unitsCol, unitlist, unitstatus, @"http://requestb.in/1k5c74z1",5);
            StatusObject resultunits = null;
            for(int i = 0; i < 20; i++)
            {
                resultunits = DocumentDBHelper.GetStatusObject(unitstatus.id);
                if(resultunits.totalRecords == resultunits.recordsDone)
                {
                    break;
                }
                System.Threading.Thread.Sleep(10 * 1000);
            }

            Assert.AreEqual(resultunits.totalRecords, resultunits.recordsDone);
        }
       

        [TestMethod]
        public void PostListSingleTestAllRecords()
        {
           
            string units = System.IO.File.ReadAllText(@"C:\Users\viho\Desktop\akelius\docdb\units.json");
            var unitsCol = "INT002_units_v1";
            var unitlist = Newtonsoft.Json.JsonConvert.DeserializeObject(units) as IEnumerable<dynamic>;
            var unitstatus = DocumentDBHelper.CreateStatusObject(unitlist.Count());

            Task<StatusObject> taskunits = DocumentDBHelper.ProcessList(unitsCol, unitlist, unitstatus);
            
            StatusObject resultunits = null;
            while(true)
            {
                resultunits = DocumentDBHelper.GetStatusObject(unitstatus.id);
                if (resultunits.totalRecords == resultunits.recordsDone + resultunits.errorRecords)
                {
                    break;
                }
                
                System.Threading.Thread.Sleep(5 * 1000);
            }

            Assert.AreEqual(resultunits.totalRecords, resultunits.recordsDone);
        }


        [TestMethod]
        public void PostListMultipleTest()
        {

            string units = System.IO.File.ReadAllText(@"YOUR_FILE_PATH_\units.json");
            var unitsCol = "INT002_units_v1";
            var unitlist = Newtonsoft.Json.JsonConvert.DeserializeObject(units) as IEnumerable<dynamic>;
            var unitstatus = DocumentDBHelper.CreateStatusObject(unitlist.Count());

            Task<StatusObject> taskunits = DocumentDBHelper.ProcessList(unitsCol, unitlist, unitstatus);

            StatusObject resultunits = null;
            while (true)
            {
                resultunits = DocumentDBHelper.GetStatusObject(unitstatus.id);
                if (resultunits.totalRecords == resultunits.recordsDone + resultunits.errorRecords)
                {
                    break;
                }

                System.Threading.Thread.Sleep(5 * 1000);
            }

   

            string rents = System.IO.File.ReadAllText(@"YOUR_FILE_PATH_\rents.json");
            var rentsCol = "INT011_rents_v1";
            var rentslist = Newtonsoft.Json.JsonConvert.DeserializeObject(rents) as IEnumerable<dynamic>;
            var rentstatus = DocumentDBHelper.CreateStatusObject(rentslist.Count());

            Task<StatusObject> taskrents = DocumentDBHelper.ProcessList(rentsCol, rentslist, rentstatus);

            StatusObject resultrents = null;
            while (true)
            {
                resultrents = DocumentDBHelper.GetStatusObject(rentstatus.id);
                if (resultrents.totalRecords == resultrents.recordsDone + resultrents.errorRecords)
                {
                    break;
                }

                System.Threading.Thread.Sleep(5 * 1000);
            }

            Assert.AreEqual(resultunits.totalRecords, resultunits.recordsDone);
            Assert.AreEqual(resultrents.totalRecords, resultrents.recordsDone);

        }
    }
}

