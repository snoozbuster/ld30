using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LD30
{
    static class OnlineHandler
    {
        public static void UploadWorld(World w)
        {
            string filename = w.SaveToFile();
            using(WebClient client = new WebClient())
            {
                client.UploadFile("http://accelerateddeliverygame.com/ld30temp/upload.php?name=" + w.OwnerName, filename);
            }
        }

        /// <summary>
        /// Don't call this on the main thread. Just... don't.
        /// </summary>
        public static paste[] DownloadWorlds()
        {
            using(WebClient client = new WebClient())
            {
                try
                {
                    byte[] xml = client.DownloadData("http://accelerateddeliverygame.com/ld30temp/download.php");
                    // make the serializer happy; all that's probably required is wrapping the returned XML in <ArrayOfPaste> tags
                    xml = Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?>\r\n<ArrayOfPaste xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">").Concat(xml.Skip(3)).Concat(Encoding.ASCII.GetBytes("</ArrayOfPaste>")).ToArray();
                    XmlSerializer serializer = new XmlSerializer(typeof(paste[]));
                    paste[] temp = (paste[])serializer.Deserialize(new MemoryStream(xml));
                    return temp;
                }
                catch
                {
                    Thread.Sleep(100000);
                    return DownloadWorlds();
                }
            }
        }
    }
}
