using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;

// This class can be used to parse the text following the program name when a 
//  program is started from the command line
//
// -------------------------------------------------------------------------------
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Program started November 8, 2003

// E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
// Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
// -------------------------------------------------------------------------------
// 
// Last modified October 5, 2016

namespace FileProcessor
{

    public class clsParseCommandLine
    {

        public const char DEFAULT_SWITCH_CHAR = '/';

        public const char ALTERNATE_SWITCH_CHAR = '-';

        public const char DEFAULT_SWITCH_PARAM_CHAR = ':';
        private readonly Dictionary<string, string> mSwitches = new Dictionary<string, string>();

        private readonly List<string> mNonSwitchParameters = new List<string>();
        private bool mShowHelp;

        private bool mDebugMode;
        public bool NeedToShowHelp
        {
            get { return mShowHelp; }
        }

        public int ParameterCount
        {
            get { return mSwitches.Count; }
        }

        public int NonSwitchParameterCount
        {
            get { return mNonSwitchParameters.Count; }
        }

        public bool DebugMode
        {
            get { return mDebugMode; }
            set { mDebugMode = value; }
        }

        /// <summary>
        /// Compares the parameter names in objParameterList with the parameters at the command line
        /// </summary>
        /// <param name="parameterList">Parameter list</param>
        /// <returns>True if any of the parameters are not present in parameterList()</returns>
        public bool InvalidParametersPresent(List<string> parameterList)
        {
            const bool caseSensitive = false;
            return InvalidParametersPresent(parameterList, caseSensitive);
        }

        /// <summary>
        /// Compares the parameter names in parameterList with the parameters at the command line
        /// </summary>
        /// <param name="parameterList">Parameter list</param>
        /// <returns>True if any of the parameters are not present in parameterList()</returns>
        public bool InvalidParametersPresent(IEnumerable<string> parameterList)
        {
            const bool caseSensitive = false;
            return InvalidParametersPresent(parameterList, caseSensitive);
        }

        /// <summary>
        /// Compares the parameter names in parameterList with the parameters at the command line
        /// </summary>
        /// <param name="parameterList">Parameter list</param>
        /// <param name="caseSensitive">True to perform case-sensitive matching of the parameter name</param>
        /// <returns>True if any of the parameters are not present in parameterList()</returns>
        public bool InvalidParametersPresent(IEnumerable<string> parameterList, bool caseSensitive)
        {
            if (InvalidParameters(parameterList.ToList()).Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool InvalidParametersPresent(List<string> validParameters, bool caseSensitive)
        {

            if (InvalidParameters(validParameters, caseSensitive).Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public List<string> InvalidParameters(List<string> validParameters)
        {
            const bool caseSensitive = false;
            return InvalidParameters(validParameters, caseSensitive);
        }

        public List<string> InvalidParameters(List<string> validParameters, bool caseSensitive)
        {
            var lstInvalidParameters = new List<string>();


            try
            {
                // Find items in mSwitches whose keys are not in validParameters)		

                foreach (var item in mSwitches)
                {
                    var itemKey = item.Key;
                    int matchCount;

                    if (caseSensitive)
                    {
                        matchCount = (from validItem in validParameters
                                      where validItem == itemKey
                                      select validItem).Count();
                    }
                    else
                    {
                        matchCount = (from validItem in validParameters
                                      where string.Equals(validItem, itemKey, StringComparison.CurrentCultureIgnoreCase)
                                      select validItem).Count();
                    }

                    if (matchCount == 0)
                    {
                        lstInvalidParameters.Add(item.Key);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error in InvalidParameters", ex);
            }

            return lstInvalidParameters;

        }

        /// <summary>
        /// Look for parameter on the command line
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <returns>True if present, otherwise false</returns>
        public bool IsParameterPresent(string paramName)
        {
            string paramValue;
            const bool caseSensitive = false;
            return RetrieveValueForParameter(paramName, out paramValue, caseSensitive);
        }

        /// <summary>
        /// Parse the parameters and switches at the command line; uses / for the switch character and : for the switch parameter character
        /// </summary>
        /// <returns>Returns True if any command line parameters were found; otherwise false</returns>
        /// <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
        public bool ParseCommandLine()
        {
            return ParseCommandLine(DEFAULT_SWITCH_CHAR, DEFAULT_SWITCH_PARAM_CHAR);
        }

        /// <summary>
        /// Parse the parameters and switches at the command line; uses : for the switch parameter character
        /// </summary>
        /// <returns>Returns True if any command line parameters were found; otherwise false</returns>
        /// <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
        public bool ParseCommandLine(char switchStartChar)
        {
            return ParseCommandLine(switchStartChar, DEFAULT_SWITCH_PARAM_CHAR);
        }

        /// <summary>
        /// Parse the parameters and switches at the command line
        /// </summary>
        /// <param name="switchStartChar"></param>
        /// <param name="switchParameterChar"></param>
        /// <returns>Returns True if any command line parameters were found; otherwise false</returns>
        /// <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
        public bool ParseCommandLine(char switchStartChar, char switchParameterChar)
        {
            // Returns True if any command line parameters were found
            // Otherwise, returns false
            //
            // If /? or /help is found, then returns False and sets mShowHelp to True

            mSwitches.Clear();
            mNonSwitchParameters.Clear();

            try
            {
                string commandLine;
                try
                {
                    // .CommandLine() returns the full command line
                    commandLine = Environment.CommandLine;

                    // .GetCommandLineArgs splits the command line at spaces, though it keeps text between double quotes together
                    // Note that .NET will strip out the starting and ending double quote if the user provides a parameter like this:
                    // MyProgram.exe "C:\Program Files\FileToProcess"
                    //
                    // In this case, paramList[1] will not have a double quote at the start but it will have a double quote at the end:
                    //  paramList[1] = C:\Program Files\FileToProcess"

                    // One very odd feature of Environment.GetCommandLineArgs() is that if the command line looks like this:
                    //    MyProgram.exe "D:\My Folder\Subfolder\" /O:D:\OutputFolder
                    // Then paramList will have:
                    //    paramList[1] = D:\My Folder\Subfolder" /O:D:\OutputFolder
                    //
                    // To avoid this problem instead specify the command line as:
                    //    MyProgram.exe "D:\My Folder\Subfolder" /O:D:\OutputFolder
                    // which gives:
                    //    paramList[1] = D:\My Folder\Subfolder
                    //    paramList[2] = /O:D:\OutputFolder
                    //
                    // Due to the idiosyncrasies of .GetCommandLineArgs, we will instead use SplitCommandLineParams to do the splitting
                    // paramList = Environment.GetCommandLineArgs()

                }
                catch (Exception ex)
                {
                    // In .NET 1.x, programs would fail if called from a network share
                    // This appears to be fixed in .NET 2.0 and above
                    // If an exception does occur here, we'll show the error message at the console, then sleep for 2 seconds

                    Console.WriteLine(@"------------------------------------------------------------------------------");
                    Console.WriteLine(@"This program cannot be run from a network share.  Please map a drive to the");
                    Console.WriteLine(@" network share you are currently accessing or copy the program files and");
                    Console.WriteLine(@" required DLL's to your local computer.");
                    Console.WriteLine(@" Exception: " + ex.Message);
                    Console.WriteLine(@"------------------------------------------------------------------------------");

                    PauseAtConsole(5000, 1000);

                    mShowHelp = true;
                    return false;
                }

                if (mDebugMode)
                {
                    Console.WriteLine();
                    Console.WriteLine(@"Debugging command line parsing");
                    Console.WriteLine();
                }

                var paramList = SplitCommandLineParams(commandLine);

                if (mDebugMode)
                {
                    Console.WriteLine();
                }

                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    return false;
                }

                if (commandLine.IndexOf(switchStartChar + "?", StringComparison.Ordinal) > 0 ||
                    commandLine.ToLower().IndexOf(switchStartChar + "help", StringComparison.CurrentCultureIgnoreCase) > 0)
                {
                    mShowHelp = true;
                    return false;
                }

                // Parse the command line
                // Note that paramList[0] is the path to the Executable for the calling program

                for (var paramIndex = 1; paramIndex <= paramList.Length - 1; paramIndex++)
                {
                    if (paramList[paramIndex].Length <= 0)
                    {
                        continue;
                    }

                    var paramName = paramList[paramIndex].TrimStart(' ');
                    var paramValue = string.Empty;

                    bool isSwitchParam;
                    if (paramName.StartsWith(switchStartChar))
                    {
                        isSwitchParam = true;
                    }
                    else if (paramName.StartsWith(ALTERNATE_SWITCH_CHAR) || paramName.StartsWith(DEFAULT_SWITCH_CHAR))
                    {
                        isSwitchParam = true;
                    }
                    else
                    {
                        // Parameter doesn't start with switchStartChar or / or -
                        isSwitchParam = false;
                    }

                    if (isSwitchParam)
                    {
                        // Look for switchParameterChar in paramList[paramIndex]
                        var charIndex = paramList[paramIndex].IndexOf(switchParameterChar);

                        if (charIndex >= 0)
                        {
                            // Parameter is of the form /I:MyParam or /I:"My Parameter" or -I:"My Parameter" or /MyParam:Setting
                            paramValue = paramName.Substring(charIndex + 1).Trim();

                            // Remove any starting and ending quotation marks
                            paramValue = paramValue.Trim('"');

                            paramName = paramName.Substring(0, charIndex);
                        }
                        else
                        {
                            // Parameter is of the form /S or -S
                        }

                        // Remove the switch character from paramName
                        paramName = paramName.Substring(1).Trim();

                        if (mDebugMode)
                        {
                            Console.WriteLine(@"SwitchParam: " + paramName + @"=" + paramValue);
                        }

                        // Note: This will add paramName if it doesn't exist (which is normally the case)
                        mSwitches[paramName] = paramValue;
                    }
                    else
                    {
                        // Non-switch parameter since switchParameterChar was not found and does not start with switchStartChar

                        // Remove any starting and ending quotation marks
                        paramName = paramName.Trim('"');

                        if (mDebugMode)
                        {
                            Console.WriteLine(@"NonSwitchParam " + mNonSwitchParameters.Count + @": " + paramName);
                        }

                        mNonSwitchParameters.Add(paramName);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error in ParseCommandLine", ex);
            }

            if (mDebugMode)
            {
                Console.WriteLine();
                Console.WriteLine(@"Switch Count = " + mSwitches.Count);
                Console.WriteLine(@"NonSwitch Count = " + mNonSwitchParameters.Count);
                Console.WriteLine();
            }

            if (mSwitches.Count + mNonSwitchParameters.Count > 0)
            {
                return true;
            }

            return false;
        }


        public static void PauseAtConsole(int millisecondsToPause, int millisecondsBetweenDots)
        {
            int totalIterations;

            Console.WriteLine();
            Console.Write(@"Continuing in " + (millisecondsToPause / 1000.0).ToString("0") + @" seconds ");

            try
            {
                if (millisecondsBetweenDots == 0)
                    millisecondsBetweenDots = millisecondsToPause;

                totalIterations = Convert.ToInt32(Math.Round(millisecondsToPause / (double)millisecondsBetweenDots, 0));
            }
            catch
            {
                // Ignore errors here
                totalIterations = 1;
            }

            var iteration = 0;
            do
            {
                Console.Write('.');

                System.Threading.Thread.Sleep(millisecondsBetweenDots);

                iteration += 1;
            } while (iteration < totalIterations);

            Console.WriteLine();

        }

        /// <summary>
        /// Returns the value of the non-switch parameter at the given index
        /// </summary>
        /// <param name="parameterIndex">Parameter index</param>
        /// <returns>The value of the parameter at the given index; empty string if no value or invalid index</returns>
        public string RetrieveNonSwitchParameter(int parameterIndex)
        {
            var paramValue = string.Empty;

            if (parameterIndex < mNonSwitchParameters.Count)
            {
                paramValue = mNonSwitchParameters[parameterIndex];
            }

            if (string.IsNullOrEmpty(paramValue))
            {
                return string.Empty;
            }

            return paramValue;

        }

        /// <summary>
        /// Returns the parameter at the given index
        /// </summary>
        /// <param name="parameterIndex">Parameter index</param>
        /// <param name="paramName">Parameter name (output)</param>
        /// <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
        /// <returns></returns>
        public bool RetrieveParameter(int parameterIndex, out string paramName, out string paramValue)
        {
            try
            {
                paramName = string.Empty;
                paramValue = string.Empty;

                if (parameterIndex < mSwitches.Count)
                {
                    var iEnum = mSwitches.GetEnumerator();

                    var switchIndex = 0;
                    while (iEnum.MoveNext())
                    {
                        if (switchIndex == parameterIndex)
                        {
                            paramName = iEnum.Current.Key;
                            paramValue = iEnum.Current.Value;
                            return true;
                        }
                        switchIndex += 1;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in RetrieveParameter", ex);
            }

            return false;

        }

        /// <summary>
        /// Look for parameter on the command line and returns its value in paramValue
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
        /// <returns>True if present, otherwise false</returns>
        public bool RetrieveValueForParameter(string paramName, out string paramValue)
        {
            return RetrieveValueForParameter(paramName, out paramValue, false);
        }

        /// <summary>
        /// Look for parameter on the command line and returns its value in paramValue
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
        /// <param name="caseSensitive">True to perform case-sensitive matching of the parameter name</param>
        /// <returns>True if present, otherwise false</returns>
        public bool RetrieveValueForParameter(string paramName, out string paramValue, bool caseSensitive)
        {

            try
            {
                paramValue = string.Empty;

                if (caseSensitive)
                {
                    if (mSwitches.ContainsKey(paramName))
                    {
                        paramValue = Convert.ToString(mSwitches[paramName]);
                        return true;
                    }

                    return false;
                }

                var result = (from item in mSwitches
                              where string.Equals(item.Key, paramName, StringComparison.CurrentCultureIgnoreCase)
                              select item).ToList();

                if (result.Count > 0)
                {
                    paramValue = result.First().Value;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in RetrieveValueForParameter", ex);
            }

        }

        private string[] SplitCommandLineParams(string commandLine)
        {
            var paramList = new List<string>();

            var indexStart = 0;
            var indexEnd = 0;

            try
            {

                if (!string.IsNullOrEmpty(commandLine))
                {
                    // Make sure the command line doesn't have any carriage return or linefeed characters
                    commandLine = commandLine.Replace("\r", " ");
                    commandLine = commandLine.Replace("\n", " ");

                    var insideDoubleQuotes = false;

                    while (indexStart < commandLine.Length)
                    {
                        // Step through the characters to find the next space
                        // However, if we find a double quote, then stop checking for spaces

                        if (commandLine[indexEnd] == '"')
                        {
                            insideDoubleQuotes = !insideDoubleQuotes;
                        }

                        if (!insideDoubleQuotes || indexEnd == commandLine.Length - 1)
                        {
                            if (commandLine[indexEnd] == ' ' || indexEnd == commandLine.Length - 1)
                            {
                                // Found the end of a parameter
                                var paramName = commandLine.Substring(indexStart, indexEnd - indexStart + 1).TrimEnd(' ');

                                if (paramName.StartsWith('"'))
                                {
                                    paramName = paramName.Substring(1);
                                }

                                if (paramName.EndsWith('"'))
                                {
                                    paramName = paramName.Substring(0, paramName.Length - 1);
                                }

                                if (!string.IsNullOrEmpty(paramName))
                                {
                                    if (mDebugMode)
                                    {
                                        Console.WriteLine(@"Param " + paramList.Count + @": " + paramName);
                                    }
                                    paramList.Add(paramName);
                                }

                                indexStart = indexEnd + 1;
                            }
                        }

                        indexEnd += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SplitCommandLineParams", ex);
            }

            return paramList.ToArray();

        }
    }

}

namespace ExtensionMethods
{
    public static class StringExtensions
    {
        /// <summary>
        /// Determine whether a string starts with a character
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ch"></param>
        /// <returns>True if str starts with ch</returns>
        public static bool StartsWith(this string str, char ch)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (str[0] == ch)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determine whether a string ends with a character
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ch"></param>
        /// <returns>True if str ends with ch</returns>
        public static bool EndsWith(this string str, char ch)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (str[str.Length - 1] == ch)
                    return true;
            }
            return false;
        }
    }
}