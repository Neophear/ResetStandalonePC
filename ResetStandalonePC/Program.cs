using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ResetStandalonePC
{
    class Program
    {
        static string userProfilePath = String.Empty;
        static string currentUser = String.Empty;

        static void Main(string[] args)
        {
            Console.Title = "Reset Standalone PC - Made by Stiig Gade";
            WriteOutput(".oO -Made by Stiig Gade- Oo.\n");

            if (IsInDomain())
            {
                WriteOutput("You are part of a domain. Run again when not in Domain. Press key to close...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            userProfilePath = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
            currentUser = UserPrincipal.Current.Name;

            WriteOutput("Delete all other users than Administrator and you? <y/[n]>");
            TimeoutReader.YesNoTimeout yntDeleteUsers = TimeoutReader.WaitForYorNwithTimeout(10);

            WriteOutput("Create new user? <y/[n]>");
            TimeoutReader.YesNoTimeout yntCreateUser = TimeoutReader.WaitForYorNwithTimeout(10);

            if (yntDeleteUsers == TimeoutReader.YesNoTimeout.Yes)
            {
                WriteOutput(EmptyOrHyphens.Empty);
                WriteOutput("Deleting users:");
                bool deleteUsersError = DeleteAllUsersExceptAdmin();
                WriteOutput("Done deleting users{0}!", (deleteUsersError ? ", but with errors" : ""));
                WriteOutput(EmptyOrHyphens.Hyphens);
                WriteOutput(EmptyOrHyphens.Empty);

                WriteOutput("Deleting user folders:");
                bool deleteUserFolderErrors = DeleteAllUserFoldersExceptAdmin(deleteUsersError);
                WriteOutput("Done deleting users folders{0}!", (deleteUserFolderErrors ? ", but with errors" : ""));

                if (deleteUserFolderErrors)
                    Process.Start(userProfilePath);

                WriteOutput(EmptyOrHyphens.Hyphens);
                WriteOutput(EmptyOrHyphens.Empty);
            }

            if (yntCreateUser == TimeoutReader.YesNoTimeout.Yes)
            {
                string newUsername = "TRR-BRUGER";
                string newPassword = "trr-bruger";

                DeactivatePasswordComplexity();

                WriteOutput(false, "New username (within 10 sec)? [{0}]: ", newUsername);
                newUsername = TimeoutReader.WaitForInputOrTimeout(10, newUsername);

                WriteOutput(false, "New password? (within 10 sec) [{0}]: ", newPassword);
                newPassword = TimeoutReader.WaitForInputOrTimeout(10, newPassword);


            }
            
            Console.ReadKey(true);
            Environment.Exit(0);
            

            ////Delete old user if exists
            //Console.Write("Deleting old user... ");
            //if (CheckIfUserExist(username))
            //{
            //    StartProcess("CMD.exe", String.Format("/C net user \"{0}\" /DEL", username));

            //    if (CheckIfUserExist(username))
            //    {
            //        do
            //        {
            //            Console.WriteLine("Error!");
            //            Console.WriteLine("  The user wasn't deleted!");
            //            Console.WriteLine("  Delete {0} manually! Then press 'c' to continue...", username);
            //            error = true;
            //            WaitForKey(ConsoleKey.C);
            //        } while (CheckIfUserExist(username));
            //    }
            //    else
            //        Console.WriteLine("Done!");
            //}
            //else
            //{
            //    Console.WriteLine("Didn't exist!");
            //}

            ////If user-profile folder exists, delete it
            //Console.Write("Deleting old folder(s)...");
            

            ////Create new user
            //Console.Write("Creating new user... ");
            //StartProcess("CMD.exe", String.Format("/C net user \"{0}\" \"{1}\" /passwordchg:no /ADD", username, password), 15);
            //StartProcess("CMD.exe", String.Format("/C WMIC USERACCOUNT WHERE \"Name='{0}'\" SET PasswordExpires=FALSE", username), 15);

            //if (CheckIfUserExist(username))
            //    Console.WriteLine("Done!");
            //else
            //{
            //    Console.WriteLine("Error!");
            //    Console.WriteLine("  New user wasn't created!");
            //    Console.WriteLine("  Create manually! Then press 'c' to continue...");
            //    WaitForKey(ConsoleKey.C);
            //    error = true;
            //}

            //bool addToAdministrator = false;
            //Console.WriteLine("Make {0} Administrator? <y/n>", username);
            //if (WaitForYorNwithTimeout(10))
            //{
            //    addToAdministrator = true;
            //    Console.Write("Adding user to Administrators... ");
            //    StartProcess("CMD.exe", String.Format("/C net localgroup Administratorer \"{0}\" /ADD", username), 15);

            //    if (IsUserInGroup(username, "Administratorer"))
            //    {
            //        Console.WriteLine("Done!");
            //    }
            //    else
            //    {
            //        Console.WriteLine("Error!");
            //        Console.WriteLine("  User wasn't added to Administrators!");
            //        Console.WriteLine("  Add manually! Then press 'c' to continue...");
            //        WaitForKey(ConsoleKey.C);
            //        error = true;
            //    }
            //}

            //if (!error)
            //{
            //    Console.WriteLine("  Username: {0}", username);
            //    Console.WriteLine("  Password: {0}", password);
            //    Console.WriteLine("  Administrator: {0}", addToAdministrator.ToString());
            //    Console.WriteLine("Press key to exit...");
            //    Console.ReadKey(true);
            //}
            //else
            //{
            //    Console.WriteLine("Error(s) have occured! Press 'e' to exit...");

            //    WaitForKey(ConsoleKey.E);
            //}
        }

        private static void WriteOutput(EmptyOrHyphens eoh)
        {
            if (eoh == EmptyOrHyphens.Empty)
                WriteOutput("");
            else
                WriteOutput("----------");
        }

        private static void WriteOutput(string text, params string[] args)
        {
            WriteOutput(true, text, args);
        }

        private static void WriteOutput(bool breakAfter, string text, params string[] args)
        {
            if (breakAfter)
                Console.WriteLine(text, args);
            else
                Console.Write(text, args);
        }
        private enum EmptyOrHyphens
        {
            Empty,
            Hyphens
        }

        private static bool DeleteAllUsersExceptAdmin()
        {
            bool deleteUsersError = false;
            string[] dontDeleteUsers = { "Administrator", "Gæst", "Guest", currentUser };

            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject envVar in searcher.Get())
            {
                string username = envVar["Name"].ToString();
                if (!dontDeleteUsers.Contains(username))
                {
                    WriteOutput(false, "Deleting user {0}... ", username);
                    StartProcess("CMD.exe", String.Format("/C net user \"{0}\" /DEL", username));

                    if (CheckIfUserExist(username))
                    {
                        WriteOutput("Error!");
                        deleteUsersError = true;
                    }
                    else
                        WriteOutput("Done!");
                }
            }

            return deleteUsersError;
        }

        private static bool DeleteAllUserFoldersExceptAdmin(bool hadDeleteUserErrors)
        {
            bool deleteFolderErrors = false;
            List<string> dontDeleteFolders = new List<string>();
            dontDeleteFolders.AddRange(new string[] { "Administrator", "All Users", "Default", "Default User", "Public", currentUser });

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            DirectoryInfo di = new DirectoryInfo(folder).Parent;

            DirectoryInfo[] subDirs = di.GetDirectories("*");

            if (hadDeleteUserErrors)
            {
                WriteOutput("One or more users weren't deleted. Delete all user folders anyway? <y/[n]>");
                TimeoutReader.YesNoTimeout ynt = TimeoutReader.WaitForYorNwithTimeout(10);

                if (ynt != TimeoutReader.YesNoTimeout.Yes)
                {
                    SelectQuery query = new SelectQuery("Win32_UserAccount");
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                    foreach (ManagementObject envVar in searcher.Get())
                        dontDeleteFolders.Add(envVar["Name"].ToString());
                }
            }

            foreach (DirectoryInfo subDir in subDirs)
            {
                if (!dontDeleteFolders.Contains(subDir.Name))
                {
                    WriteOutput(false, "Deleting userfolder {0}... ", subDir.Name);
                    StartProcess("CMD.exe", String.Format("/C RD /S /Q \"{0}\"", subDir.FullName), 15);
                    
                    if (Directory.Exists(subDir.FullName))
                    {
                        deleteFolderErrors = true;
                        WriteOutput("Error!");
                    }
                    else
                        WriteOutput("Done!");
                }
            }

            return deleteFolderErrors;
        }

        /// <summary>
        /// Checks if a user is in a group.
        /// </summary>
        /// <param name="username">The user to add</param>
        /// <param name="group">The group to add to</param>
        /// <returns>True if user is in group</returns>
        private static bool IsUserInGroup(string username, string group)
        {
            bool result = false;

            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal up = UserPrincipal.FindByIdentity(
                    pc,
                    IdentityType.SamAccountName,
                    username);

                GroupPrincipal gp = GroupPrincipal.FindByIdentity(
                    pc,
                    IdentityType.SamAccountName,
                    group);

                result = up.IsMemberOf(gp);
            }

            return result;
        }

        /// <summary>
        /// Waits for the correct keypress
        /// </summary>
        /// <param name="key">Key to be pressed</param>
        private static void WaitForKey(ConsoleKey key)
        {
            while (true)
                if (Console.ReadKey(true).Key == key)
                    break;
        }

        /// <summary>
        /// Check if local username exists
        /// </summary>
        /// <param name="username">The username to check</param>
        /// <returns></returns>
        private static bool CheckIfUserExist(string username)
        {
            bool userExist = false;

            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal up = UserPrincipal.FindByIdentity(
                    pc,
                    IdentityType.SamAccountName,
                    username);
                
                userExist = (up != null);
            }

            return userExist;
        }

        //private static bool IsInDomain()
        //{
        //    //Check to see whether you are part of a domain
        //    bool isInDomain = false;

        //    try
        //    {
        //        System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();
        //        isInDomain = true;
        //    }
        //    catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException)
        //    {

        //    }

        //    return isInDomain;
        //}

        private static void DeactivatePasswordComplexity()
        {
            bool passwordComplexity = false;

            //Create temporary file to extract the Local Security Settings
            string tempCFG = Path.GetTempFileName();
            WriteOutput(false, "Creating temporary CFG file... ");

            try
            {
                StartProcess(@"%SystemRoot%\system32\secedit.exe", String.Format("/export /cfg \"{0}\" /quiet", tempCFG));
                WriteOutput("Done!");
            }
            catch (Exception e)
            {
                WriteOutput("Error!");
                WriteOutput("  Something went wrong. Press 'm' to view message or 'e' to exit...");
                while (true)
                {
                    switch (Console.ReadKey(true).Key)
                    {
                        case ConsoleKey.M:
                            WriteOutput(e.Message);
                            WriteOutput("  Press 'e' to exit...");
                            WaitForKey(ConsoleKey.E);
                            Environment.Exit(1);
                            break;
                        case ConsoleKey.E:
                            Environment.Exit(1);
                            break;
                        default:
                            break;
                    }
                }
            }

            //Read and edit the PasswordComplexity in the temporary CFG file
            StringBuilder newCfg = new StringBuilder();
            string[] cfg = File.ReadAllLines(tempCFG);

            foreach (string line in cfg)
            {
                if (line.Contains("PasswordComplexity"))
                {
                    passwordComplexity = line.Contains("1");

                    newCfg.AppendLine(line.Replace("1", "0"));
                    continue;
                }
                newCfg.AppendLine(line);
            }

            //If PasswordComplexity was active then deactivate by importing the edited CFG-file
            if (passwordComplexity)
            {
                File.WriteAllText(tempCFG, newCfg.ToString());

                WriteOutput("Deactivating PasswordComplexity... ");

                try
                {
                    StartProcess(@"%SystemRoot%\system32\secedit.exe", String.Format("/configure /db secedit.sdb /cfg \"{0}\" /quiet", tempCFG));
                    WriteOutput("Done!");
                }
                catch (Exception e)
                {
                    WriteOutput("Error!");
                    WriteOutput("  Something went wrong. Press 'm' to view message or 'e' to exit...");
                    while (true)
                    {
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.M:
                                WriteOutput(e.Message);
                                WriteOutput("  Press 'e' to exit...");
                                WaitForKey(ConsoleKey.E);
                                Environment.Exit(1);
                                break;
                            case ConsoleKey.E:
                                Environment.Exit(1);
                                break;
                            default:
                                break;
                        }
                    }
                }

                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (File.Exists(path + "\\secedit.sdb"))
                    File.Delete(path + "\\secedit.sdb");
            }
        }

        #region StartProcess
        /// <summary>
        /// Start new process
        /// </summary>
        /// <param name="processToStart">The process to start. Fx. "CMD.exe"</param>
        private static void StartProcess(string processToStart)
        {
            StartProcess(processToStart, "", 0);
        }
        /// <summary>
        /// Start new process
        /// </summary>
        /// <param name="processToStart">The process to start. Fx. "CMD.exe"</param>
        /// <param name="waitTime">Max timeout to wait. 0 to indefinetly. -1 not to wait</param>
        private static void StartProcess(string processToStart, int waitTime)
        {
            StartProcess(processToStart, "", waitTime);
        }
        /// <summary>
        /// Start new process
        /// </summary>
        /// <param name="processToStart">The process to start. Fx. "CMD.exe"</param>
        /// <param name="arguments">Arguments to pass. Fx. "ipconfig /release"</param>
        private static void StartProcess(string processToStart, string arguments)
        {
            StartProcess(processToStart, arguments, 0);
        }
        /// <summary>
        /// Start new process
        /// </summary>
        /// <param name="processToStart">The process to start. Fx. "CMD.exe"</param>
        /// <param name="arguments">Arguments to pass. Fx. "ipconfig /release"</param>
        /// <param name="waitTime">Max timeout to wait. 0 to indefinetly. -1 not to wait</param>
        private static void StartProcess(string processToStart, string arguments, int waitTime)
        {
            if (waitTime > 0)
                waitTime *= 1000;

            Process p = new Process();
            p.StartInfo.FileName = Environment.ExpandEnvironmentVariables(processToStart);

            if (arguments != String.Empty)
                p.StartInfo.Arguments = arguments;

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            if (waitTime == 0)
                p.WaitForExit();
            else if (waitTime > 0)
                p.WaitForExit(waitTime);
        }
        #endregion

        public static bool IsInDomain()
        {
            Win32.NetJoinStatus status = Win32.NetJoinStatus.NetSetupUnknownStatus;
            IntPtr pDomain = IntPtr.Zero;
            int result = Win32.NetGetJoinInformation(null, out pDomain, out status);

            if (pDomain != IntPtr.Zero)
                Win32.NetApiBufferFree(pDomain);
            if (result == Win32.ErrorSuccess)
                return status == Win32.NetJoinStatus.NetSetupDomainName;
            else
                throw new Exception("Domain Info Get Failed");
        }
        
    }

    public class TimeoutReader
    {
        public static YesNoTimeout WaitForYorNwithTimeout(int timeOutSec)
        {
            timeOutSec *= 1000;

            DateTime timeoutvalue = DateTime.Now.AddMilliseconds(timeOutSec);

            int counter = timeOutSec / 100;

            while (DateTime.Now < timeoutvalue)
            {
                int rest = counter-- % 10;

                if (rest == 0)
                    Console.Write((counter + 1) / 10);
                else if (rest == 3 || rest == 6)
                    Console.Write(".");

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Y)
                    {
                        Console.WriteLine();
                        return YesNoTimeout.Yes;
                    }
                    else if (cki.Key == ConsoleKey.N)
                        return YesNoTimeout.No;
                    else
                        System.Threading.Thread.Sleep(100);
                }
                else
                    System.Threading.Thread.Sleep(100);
            }

            Console.WriteLine();
            return YesNoTimeout.Timeout;
        }

        public enum YesNoTimeout
        {
            Yes,
            No,
            Timeout
        }

        #region WaitForInputOrTimeout
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }

        public static string WaitForInputOrTimeout(int timeOutsec, string defaultOnTimeout)
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            Thread inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
            input = defaultOnTimeout;

            getInput.Set();
            bool success = gotInput.WaitOne(timeOutsec * 1000);

            return String.IsNullOrWhiteSpace(input) ? defaultOnTimeout : input;
        }
        #endregion
    }

    public class Win32
    {
        public const int ErrorSuccess = 0;

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

        [DllImport("Netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        public enum NetJoinStatus
        {
            NetSetupUnknownStatus = 0,
            NetSetupUnjoined,
            NetSetupWorkgroupName,
            NetSetupDomainName
        }

    }
}