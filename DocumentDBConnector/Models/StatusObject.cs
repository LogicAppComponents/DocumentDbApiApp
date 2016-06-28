using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentDBConnector.Models
{
    public class StatusObject
    {

        public StatusObject()
        {

        }

        public StatusObject(string errormsg)
        {
            this.errorMsg = errorMsg;
        }
        private object lockobject = new object();

        public Guid id = Guid.NewGuid();
        private int _recordsDone;
        public int _totalRecords;
        public int totalRecords
        {
            get
            {
                lock (lockobject)
                {
                    return _totalRecords;
                }
            }
            set
            {
                lock (lockobject)
                {
                    _totalRecords = value;
                }
            }
        }
        public void IncreasFailedRecords()
        {
            lock (lockobject)
            {
                _totalRecords++;
                lastUpdate = DateTime.Now;
            }
        }
        public int _failedRecords;
        public int failedRecords
        {
            get
            {
                lock (lockobject)
                {
                    return _failedRecords;
                }
            }
            set
            {
                lock (lockobject)
                {
                    _failedRecords = value;
                }
            }
        }
        public void IncreaseFailedRecords()
        {
            lock (lockobject)
            {
                _failedRecords++;
                lastUpdate = DateTime.Now;
            }
        }
        public int _errorRecords;
        public int errorRecords
        {
            get
            {
                lock (lockobject)
                {
                    return _errorRecords;
                }
            }
            set
            {
                lock (lockobject)
                {
                    _errorRecords = value;
                }
            }
        }
        public void IncreaseErrorRecords()
        {
            lock (lockobject)
            {
                _errorRecords++;
                lastUpdate = DateTime.Now;
            }
        }
        public int recordsDone
        {
            get
            {
                lock (lockobject)
                {
                    return _recordsDone;
                }
            }
            set
            {
                lock (lockobject)
                {
                    _recordsDone = value;
                }
            }
        }
        public void IncreasRecordsDone()
        {
            lock (lockobject)
            {                
                _recordsDone++;
                lastUpdate = DateTime.Now;
            }
        }

        
        public string errorMsg
        {
            get
            {
                lock(lockobject)
                {
                    return _errorMsg;
                }
            }
            set
            {
                lock(lockobject)
                {
                    _errorMsg = value;
                }
            }
        }
        private string _errorMsg;
        public DateTime lastUpdate;
        public double reqCharge;

    }
}