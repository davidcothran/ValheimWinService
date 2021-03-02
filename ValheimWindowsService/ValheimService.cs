using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ValheimWindowsService
{
    public partial class ValheimService : ServiceBase
    {
        string configFile = @"C:\valheim\valheimservice.config";
        double time_interval = 86400000;
        double times_per_day = 3;

        string servicePath = @"C:\valheimservice";

        string valheimData = @"\..\LocalLow\IronGate\Valheim";

        string steam_cmd_path = @"C:\steamcmd";
        string backup_path = @"C:\valheimbackup";
        string update_batch = @"C:\valheim\update.bat";
        bool delete_old_backups = false;
        int number_of_backups = 12;
        string dateStamp;

        List<string> configList;


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        public ValheimService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            dateStamp = DateTime.Now.ToString("-dd-MM-yyyy-(hh-mm-ss)");
            System.IO.Directory.CreateDirectory(servicePath);
            File.AppendAllText(servicePath + @"\log.txt", "Starting " + dateStamp + "\n");

            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            File.AppendAllText(servicePath + @"\log.txt", "AppData " + appdata + "\n");

            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            File.AppendAllText(servicePath + @"\log.txt", "Pending " + dateStamp + "\n");

            ParseConfig();

            File.AppendAllText(servicePath + @"\log.txt", "Parsed " + dateStamp + "\n");

            // Set up a timer that triggers every interval.
            Timer timer = new Timer();
            timer.Interval = time_interval;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            File.AppendAllText(servicePath + @"\log.txt", "Start Backup : " + dateStamp + "\n");
            // Run backup and update on startup
            BackupAndUpdate();

            File.AppendAllText(servicePath + @"\log.txt", "Finished backup, starting service : " + dateStamp + "\n");
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            dateStamp = DateTime.Now.ToString("-dd-MM-yyyy-(hh-mm-ss)");
            BackupAndUpdate();
        }

        /// <summary>
        /// Function which handles backing up files and checking for updates. If update is found, will end server and update.
        /// </summary>
        private void BackupAndUpdate()
        {
            // Backup World
            CreateBackup();

            // Check for game updates
            // ..\steamcmd\steamcmd.exe +login anonymous +app_info_print 896660
            //      Go to Branches/Public/BuildID
            // C:\valheim\steamapps\appmanifest_896660.acf 
            //      Go to BuildID
            // Compare these
            // If they dont match, update the server
        }


        /// <summary>
        /// Creates backup directory if it doesnt exist. 
        /// Creates sub directory as "Backup [DateTime]"
        /// Copies Valheim World Data to backup directory
        /// If applicable, deletes old backups to save memory
        /// </summary>
        private void CreateBackup()
        {
            // Make sure backup folder is made
            System.IO.Directory.CreateDirectory(backup_path);

            // Create backup location
            // Create Backup [date] folder
            string pathString = System.IO.Path.Combine(backup_path, "Backup" + dateStamp);

            File.AppendAllText(servicePath + @"\log.txt", "Creating Dir : " + pathString + "\n");
            System.IO.Directory.CreateDirectory(pathString);

            // Copy all subdirectories from Valheim Worlds
            string valheimPath = valheimData;
            File.AppendAllText(servicePath + @"\log.txt", "Valheim Data Path : " + valheimPath+ "\n");
            
            DirectoryCopy(valheimPath, pathString, true);

            // Delete old if applicable
            if (delete_old_backups)
            {
                var sortedFiles = new DirectoryInfo(backup_path).GetDirectories().OrderBy(t => t.LastWriteTime).ToList();
                // Remove oldest files
                while (sortedFiles.Count > number_of_backups)
                {
                    Directory.Delete(sortedFiles[0].FullName);
                    sortedFiles.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Reads config file. Calls function to parse
        /// </summary>
        private void ParseConfig()
        {
            // Read each line of the file into a string array. Each element of the array is one line of the file.
            if (File.Exists(configFile))
            {

                File.AppendAllText(servicePath + @"\log.txt", "Found Config"+ "\n");
                string[] lines = System.IO.File.ReadAllLines(configFile);
                configList = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || String.IsNullOrEmpty(line))
                    {
                        // Ignore empty lines and comments
                        continue;
                    }
                    else
                    {
                        File.AppendAllText(servicePath + @"\log.txt", "Config: " + line + dateStamp + "\n");
                        configList.Add(line);
                    }
                }

                PopulateConfigVariables();
            }
        }

        /// <summary>
        /// Parses each variable out of the config file.
        /// </summary>
        private void PopulateConfigVariables()
        {

            File.AppendAllText(servicePath + @"\log.txt", "Populating: " + dateStamp + "\n");
            try
            {
                // Get Valheim World Data Path
                string toParse = configList.Where(s => s.StartsWith("Valheim")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    valheimData = command;
                }

                // Get SteamCMD Path
                toParse = configList.Where(s => s.StartsWith("SteamCMD")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    steam_cmd_path = command;
                }

                // Get Backup Path
                toParse = configList.Where(s => s.StartsWith("Backup")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    backup_path = command;
                }

                // Get Update Batch File
                toParse = configList.Where(s => s.StartsWith("Update")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    update_batch = command;
                }

                // Get times per day
                toParse = configList.Where(s => s.StartsWith("Times")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    int tryResult = 0;

                    if (Int32.TryParse(command, out tryResult))
                    {
                        times_per_day = tryResult;
                    }
                }

                // Get Delete old backups
                toParse = configList.Where(s => s.StartsWith("Delete")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    delete_old_backups = (command.Equals("Y") || command.Equals("1"));
                }

                // Get Number of backups
                toParse = configList.Where(s => s.StartsWith("Number")).FirstOrDefault();
                if (!String.IsNullOrEmpty(toParse))
                {
                    string command = GetValueFromConfig(toParse);
                    File.AppendAllText(servicePath + @"\log.txt", "Command : " + command + dateStamp + "\n");
                    int tryResult = 0;

                    if (Int32.TryParse(command, out tryResult))
                    {
                        number_of_backups = tryResult;
                    }
                }
            }
            catch(Exception e)
            {
                File.AppendAllText(servicePath + @"\log.txt", "Something bad happened : " + dateStamp + "\n");
                // Fuck it i dont care will just use defaults
            }

            double milliseconds_24_hrs = 24 * 60 * 60 * 1000;
            time_interval = milliseconds_24_hrs / times_per_day;

            File.AppendAllText(servicePath + @"\log.txt", "Done Populating : " + dateStamp + "\n");
        }

        /// <summary>
        /// Helper function to parse each config line
        /// </summary>
        /// <param name="toParse">Config line to parse</param>
        /// <returns>value string from config</returns>
        private string GetValueFromConfig(string toParse)
        {
            return toParse.Split('=')[1].Trim(' ', '"');
        }


        /// <summary>
        /// Helper function to copy directories
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            File.AppendAllText(servicePath + @"\log.txt", "Copying : " + dir.FullName + "\n");
            File.AppendAllText(servicePath + @"\log.txt", sourceDirName + "\n");
            File.AppendAllText(servicePath + @"\log.txt", dir.FullName + "\n");

            if (dir.Exists)
            {
                File.AppendAllText(servicePath + @"\log.txt", "Dir Does Exist : " + dir.FullName + "\n");
                DirectoryInfo[] dirs = dir.GetDirectories();

                // If the destination directory doesn't exist, create it.       
                Directory.CreateDirectory(destDirName);


                File.AppendAllText(servicePath + @"\log.txt", "Created : " + destDirName+ "\n");

                // Get the files in the directory and copy them to the new location.
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    File.AppendAllText(servicePath + @"\log.txt", "Copying : " + tempPath+ "\n");
                    file.CopyTo(tempPath, false);
                }

                // If copying subdirectories, copy them and their contents to new location.
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        File.AppendAllText(servicePath + @"\log.txt", "Copying : " + subdir.FullName+ "\n");
                        string tempPath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to execute a batch file
        /// </summary>
        /// <param name="command"></param>
        public void ExecuteCommand(string command)
        {
            // TODO Incomplete
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process process;

            ProcessInfo = new ProcessStartInfo("\\txtmanipulator\\txtmanipulator.bat", command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            ProcessInfo.WorkingDirectory = "\\txtmanipulator";
            // *** Redirect the output ***
            ProcessInfo.RedirectStandardError = true;
            ProcessInfo.RedirectStandardOutput = true;

            process = Process.Start(ProcessInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            ExitCode = process.ExitCode;
            process.Close();
        }

    }
}
