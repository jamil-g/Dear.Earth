using ShortReportGen.Models;
using System;

namespace ShortReportGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Html2Image html2Image = new Html2Image();
            Html2Image.Coordinates coor = new Html2Image.Coordinates {lat = 32.804771, lon = 34.973730};
            double[] arr = new double[6] { 5.5, 3, 8, 2, 6.5, 3.8 };
            string addrress = "Kabirim 28, Haiafa Israel";
            string customer = "Jamil";
            string Project = "Test";
            string refno = "49399hh";
            string MapFile = ""; //"file:///C:/Users/jamil-g/Desktop/map.jpg";
            string report = html2Image.CustomizeReport(coor, customer, Project, addrress, arr, MapFile, refno);
            EmailInfo EmailInfo = new EmailInfo();
            EmailInfo.Sender = "report@dera.earth";
            EmailInfo.Recipients = "jamil.garzuzi@gmail.com";
            EmailInfo.Subject = $"Dear.Earth Indcies report of location: {addrress} - " + coor.lat.ToString() + "," + coor.lon.ToString();
            EmailInfo.EmailMsg = $"Dear {customer}, <br><br> Thank you for choosing Dear.Earth to Calculate your environmental Indcies. <br>" +
                $"Please find attached the requested information of location: {addrress} - Coordinates:[" + coor.lat.ToString() + "," + coor.lon.ToString() + "].<br><br>" +
                "Best Regards, <br> Dear.Earth Team";
            EmailInfo.Attachment = $@"C:\Users\jamil-g\source\repos\ShortReportGen\ShortReportGen\bin\Debug\netcoreapp2.1\{report}";
            StmpEmail email = new StmpEmail();
            email.SendEmailAsync(EmailInfo);
        }

    }
}
