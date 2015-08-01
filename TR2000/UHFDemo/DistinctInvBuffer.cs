using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UHFDemo
{
    class DistinctInvBuffer
    {
        //public Dictionary<String,String> EpcCollections = new Dictionary<string,string>();
        public HashSet<string> EpcCollections = new HashSet<string>();
        public void collectEpc(string epc)
        {
            EpcCollections.Add(epc);
        }
        public void clearBuffer()
        {
            EpcCollections.Clear();
        }
        public bool contains(String epc)
        {
            return EpcCollections.Contains(epc);
        }
        public int size()
        {
            return EpcCollections.Count;
        }
    }
}
