using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ServiceProcess;

/*
 * Author: David Moral Inglada
 * LinkedIn: https://www.linkedin.com/in/davidmoralinglada
 * Web: http://web4x4.es/
 * Project: Filezilla Server Autoban 
 * Language: C#
 * 
 * */

namespace FileZillaServerAutoban
{
    class Program
    {
        static void Main(string[] args)
        {
            var dateAndTime = DateTime.Now;
            var date = dateAndTime.Date;
            string[] allIps;

            //Windows Installation path
            string logpath = @"C:\Program Files (x86)\FileZilla Server\Logs\";

            //Todays log file, i schedule the exe to execute at 23:55 on the server
            string TextFileName = logpath + "fzs-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + ".log";
                      
            //We have to copy te log file because its used by the Filezilla Service 
            System.IO.File.Copy(TextFileName, logpath + "fileautoban.log", true);

            //Create a copy to read
            string TextFilePath = logpath + "fileautoban.log";

            //Variables to control de Ip´s that fail on the server
            string ip="";
            string ips="";
            string lastip="";

            Console.WriteLine("Reading log");

            //Read all lines from log, matching if logging fails.
            foreach (var line in File.ReadLines(TextFilePath))
            {
                if (line.Contains("530 Login or password incorrect!"))
                {
                    //Filter the ip from the line string
                    ip= line.Substring(line.LastIndexOf('(') + 1,15);
                    ip = ip.Replace(")", "");
                    ip = ip.Replace(">", "");
                    ip = ip.Replace(" ", "");
                    
                    //First step to prevent duplicates
                    if (lastip != ip)
                    {
                        lastip = ip;
                        ips = ips + " " + lastip;
                    }

                   
                    
                }
                
            }
            Console.WriteLine("Reading XML");

            //Now we have to save the ip´s on Filezilla Server XML Configuration File
            string xmlpath = @"C:\Program Files (x86)\FileZilla Server\FileZilla Server.xml";

            //First step: Read the value of this item
            XDocument xmlDoc = XDocument.Load(xmlpath);
            var items = (from item in xmlDoc.Descendants("Item")
                         where item.FirstAttribute.Value.ToString().Contains("IP Filter Disallowed")
                         select item).ToList();

            foreach (var item in items)
            {
                string _allips = item.Value + ips;     
                
                //Create an array with old value and new ip´s, and return a new array without duplicates           
                allIps = _allips.Split(new char[0]).Distinct().ToArray();

                //change the item XML value
                item.Value = string.Join(" ", allIps);
              
            }
            //SAve XML file

            xmlDoc.Save(xmlpath);
            Console.WriteLine("Saving XML");

            //Delete the log copy
            if (System.IO.File.Exists(logpath + "fileautoban.log"))
            {
                // Use a try block to catch IOExceptions, to
                // handle the case of the file already being
                // opened by another process.
                try
                {
                    System.IO.File.Delete(logpath + "fileautoban.log");
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            //Now we have to restart the service to read the new ip filter on configuration XML. 
            //I put 5 sec between stop and start

            string serviceName = "FileZilla Server";
            double timeoutMilliseconds = 5000;
            ServiceController service = new ServiceController(serviceName);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                Console.WriteLine("Stopping Filezila Server Service.....");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                Console.WriteLine("Starting Filezila Server Service.....");
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                Console.WriteLine(Ex.StackTrace);
                
            }
        }
    }
}
