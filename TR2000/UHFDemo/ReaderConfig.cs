using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
namespace UHFDemo
{
    [SerializableAttribute]
    public class ReaderConfig
    {       
        public enum OPERA_MODE {
            SECURITY = 135,
            STORAGE = 239
        };
        public Int32 ReaderPower;
        public Int32 InvDuration;
        public Int32 SecureInvDuration;
        public Int32 InfraredFreq;
        public Int32 AntPort;
        public string ReaderPort;
        public string PR9000Port;
        public string InfraredPort;
        public string LedControlPort;
        public Int32 LedSwitchFreq;
        public string FormTitle;
        public bool LogOn;
        public byte operaMode;
        public Int32 AlarmDuration;
        public string BasePath;
        public string BaseUserPath;
        public ReaderConfig()
        {
            operaMode = (byte)OPERA_MODE.STORAGE;
            ReaderPower = 30;
            InvDuration = 4;  //resolution is 1000ms
            SecureInvDuration = 2; //resolution is 1000ms
            InfraredFreq = 3; //resolution is 100ms
            LedSwitchFreq = 3;
            AlarmDuration = 2; //resolution is 1000ms
            AntPort = 0x3;
            ReaderPort = "COM7";
            PR9000Port = "COM5";
            InfraredPort = "COM6";
            LedControlPort = "COM4";
            FormTitle = "山脊安防通道管理系统";
            LogOn = true;
            BasePath = "c:\\communicationModule\\bin";
            BaseUserPath = "c:\\test\\backup";
        }
    }


    public class HelperConfigSerialize
    {
        public ReaderConfig oData;
        
        public readonly string fileName = "AppConfig" + ".xml";

        public HelperConfigSerialize(ReaderConfig data)
        {
            oData = data;
        }

        public bool LoadConfig()
        {
            try
            {
                using (Stream stream = new FileStream(fileName, FileMode.Open))
                {
                    // Deserialize SioData
                    IFormatter formatter = new SoapFormatter();
                    ReaderConfig sData = (ReaderConfig)formatter.Deserialize(stream);
                    oData = sData;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void SaveConfig()
        {
            using (Stream stream = new FileStream(fileName, FileMode.Create))
            {
                // Serialize SioData
                IFormatter formatter = new SoapFormatter();
                //formatter.Serialize(stream, new SioData(oData));
                formatter.Serialize(stream, oData);
            }
        }

    }
}
