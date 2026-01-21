using System;
using System.IO;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using DataScope.Select.Api.Extractions;

namespace DownloadScheduledExtractedFiles
{
    class Program
    {
        private CommandLineParser.CommandLineParser cmdParser = new CommandLineParser.CommandLineParser()
        { IgnoreCase = true };
        ValueArgument<string> dssUserName = new ValueArgument<string>('u', "username", "DSS Username") 
        {Optional=false };
        ValueArgument<string> dssPassword = new ValueArgument<string>('p', "password", "DSS Password") 
        { Optional = false };
        ValueArgument<string> scheduleName = new ValueArgument<string>('s', "schedulename", "A schedule name") 
        { Optional = false };
        EnumeratedValueArgument<string> fileType = new EnumeratedValueArgument<string>('f', "file", "Type of files (all, note, ric, data)", new string[] { "all", "note", "ric", "data" }) 
        { IgnoreCase = true, Optional=true, DefaultValue="all" };

        SwitchArgument awsFlag = new SwitchArgument('x', "aws", "Set whether show or not", false)
        {Optional=true };
        //SwitchArgument traceFlag = new SwitchArgument('d', "dump", "Dump HTTP requests", false)
        //{Optional=true};
        Uri dssUri = new Uri("https://selectapi.datascope.lseg.com/RestApi/v1/");
        ExtractionsContext extractionsContext = null;


        static void Main(string[] args)
        {
            Program prog = new Program();
            if (prog.Init(ref args))
            {
                try
                {
                    prog.Run();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            
        }

        public Program()
        {
            cmdParser.Arguments.Add(dssUserName);
            cmdParser.Arguments.Add(dssPassword);
            cmdParser.Arguments.Add(scheduleName);
            cmdParser.Arguments.Add(fileType);
            cmdParser.Arguments.Add(awsFlag);
            
        }
        public bool Init(ref string[] args)
        {
            if (args.Length == 0)
            {
                cmdParser.ShowUsage();
                return false;
            }

            try
            {
                cmdParser.ParseCommandLine(args);

                if (!cmdParser.ParsingSucceeded)
                {
                    cmdParser.ShowUsage();
                    return false;
                }

               
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.Message);
                cmdParser.ShowUsage();
                return false;
            }

            Console.WriteLine($"Download the latest {fileType.Value} extraction file(s) from the schedule {scheduleName.Value}\n");
            return true;
        }

        private void GetCredential()
        {
            if (dssUserName.Value == "")
            {
                Console.Write("Enter DSS UserName: ");
                dssUserName.Value = Console.ReadLine();
            }

            if (dssPassword.Value == "")
            {
                Console.Write("Enter DSS Password: ");
                ConsoleKeyInfo key;

                do
                {
                    key = Console.ReadKey(true);
                    // Backspace Should Not Work
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        dssPassword.Value += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && dssPassword.Value.Length > 0)
                        {
                            dssPassword.Value = dssPassword.Value.Substring(0, (dssPassword.Value.Length - 1));
                            Console.Write("\b \b");
                        }
                    }


                }
                // Stops Receving Keys Once Enter is Pressed
                while (key.Key != ConsoleKey.Enter);
            }

        }
        private void CreateExtractionsContext()
        {
            extractionsContext = new ExtractionsContext(dssUri, dssUserName.Value, dssPassword.Value);

            //WebProxy proxy = new WebProxy("http://127.0.0.1:8080/", true);
            // proxy.Credentials = new NetworkCredential("rdc", "reuters");
            // proxy.Credentials = CredentialCache.DefaultCredentials;
            //extractionsContext.Options.Proxy = proxy;
            //extractionsContext.Options.Proxy = WebRequest.DefaultWebProxy;
            // extractionsContext.Options.Proxy.Credentials = CredentialCache.DefaultCredentials;
            //extractionsContext.Options.UseProxy = true;
            Console.WriteLine("Token: {0}", extractionsContext.SessionToken);

        }
        private DataScope.Select.Api.Extractions.Schedules.Schedule GetScheduleByName(string name)
        {

            try
            {
                var schedule = extractionsContext.ScheduleOperations.GetByName(name);
                Console.WriteLine($"\nThe schedule ID of {schedule.Name} is {schedule.ScheduleId}.");
                return schedule;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n#### {scheduleName.Value} not found");
                return null;
            }

        }
        private void ListAllSchedule()
        {
            Console.WriteLine("\nAvailable Schedules:");
            var allSchedules = extractionsContext.ScheduleOperations.GetAll();
            foreach (var schedule_tmp in allSchedules)
            {
                if (schedule_tmp.Trigger.ToString() != "DataScope.Select.Api.Extractions.Schedules.ImmediateTrigger")
                    Console.WriteLine($"-\t {schedule_tmp.Name}");
            }
           
        }
        public void Run()
        {
            GetCredential();
            if (extractionsContext == null) CreateExtractionsContext();

            if (String.IsNullOrEmpty(scheduleName.Value))
            {
                Console.WriteLine("\n#### Schedule is empty.");
                ListAllSchedule();
                return;
            }

            var schedule = GetScheduleByName(scheduleName.Value);

            if(schedule == null)
            {
                ListAllSchedule();
                return;

            }
            
            extractionsContext.LoadProperty(schedule, "LastExtraction");

            Console.WriteLine($"\nThe last extraction was extracted on {schedule.LastExtraction.ExtractionDateUtc} GMT");

            extractionsContext.LoadProperty(schedule.LastExtraction, "Files");

            if (awsFlag.Value == true)
            {
                Console.WriteLine("\nDownload from AWS");
                extractionsContext.DefaultRequestHeaders.Add("X-Direct-Download", "true");
            }

            if (fileType.Value == "all")
            {
                if(schedule.LastExtraction.Files.Count == 0)
                {
                    Console.WriteLine($"\nNo file for this extraction {schedule.LastExtraction.ReportExtractionId} in this schedule {schedule.Name}");
                    return;
                }
                foreach (var file in schedule.LastExtraction.Files)
                {
                    
                    Console.WriteLine($"\n{file.ExtractedFileName} ({file.Size} bytes) is available on the server.");
                    var readStream = extractionsContext.GetReadStream(file);
                    Console.WriteLine($"{file.ExtractedFileName} has been created on the machine.");
                    using (var fileStream = File.Create(file.ExtractedFileName))
                    {
                        Console.WriteLine($"Downloading a file ...");
                        readStream.Stream.CopyTo(fileStream);
                    }
                    Console.WriteLine($"Download completed.");

                }
            }
            else
            {
                DataScope.Select.Api.Extractions.ReportExtractions.ExtractedFile file = null;
                switch (fileType.Value)
                {
                    case "note":
                        extractionsContext.LoadProperty(schedule.LastExtraction, "NotesFile");                        
                        file = schedule.LastExtraction.NotesFile;
                        break;
                    case "ric":
                        extractionsContext.LoadProperty(schedule.LastExtraction, "RicMaintenanceFile");                        
                        file = schedule.LastExtraction.RicMaintenanceFile;
                        break;
                    case "data":
                        extractionsContext.LoadProperty(schedule.LastExtraction, "FullFile");
                        file = schedule.LastExtraction.FullFile;
                        break;
                }

                if(file == null)
                {
                    Console.WriteLine($"\nA {fileType.Value} file type is not available on the server.");
                    return;
                }
                Console.WriteLine($"\n{file.ExtractedFileName} ({file.Size}) bytes is available on the server.");
                var readStream = extractionsContext.GetReadStream(file);
                Console.WriteLine($"{file.ExtractedFileName} has been created on the machine.");
                using (var fileStream = File.Create(file.ExtractedFileName))
                {
                    Console.WriteLine($"Downloading a file ...");
                    readStream.Stream.CopyTo(fileStream);
                }
                Console.WriteLine($"Download completed.");
            }           
            


        }
    }
}

