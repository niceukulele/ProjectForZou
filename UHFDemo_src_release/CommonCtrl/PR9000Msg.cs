using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonCtrl
{
    public class PR9000Msg
    {
        private byte preamble; //0xAA
        private byte msgType;
        private byte cmd;
        private short len;
        //private byte len_l;
        private byte[] payload;
        private byte endMark; //0x7E
        private byte[] rawdata;

        public PR9000Msg(byte[] data)
        {
            int nLen = data.Length;
            rawdata = new byte[len];
            data.CopyTo(rawdata, 0);
            preamble = rawdata[0];
            msgType = rawdata[1];
            cmd = rawdata[2];
            len = (short)((rawdata[3] << 8) | rawdata[4]);
            if (len > 0)
            {
                payload = new byte[len];
                Array.Copy(rawdata, 5, payload, 0, len);
            }
            endMark = rawdata[nLen - 1];
        }
    }
}
