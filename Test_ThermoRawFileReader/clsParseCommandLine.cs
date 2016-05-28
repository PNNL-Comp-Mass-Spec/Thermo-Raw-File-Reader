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
// Last modified May 26, 2016

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
		/// <param name="objParameterList">Parameter list</param>
		/// <returns>True if any of the parameters are not present in strParameterList()</returns>
		public bool InvalidParametersPresent(List<string> objParameterList)
		{
			const bool blnCaseSensitive = false;
			return InvalidParametersPresent(objParameterList, blnCaseSensitive);
		}

		/// <summary>
		/// Compares the parameter names in strParameterList with the parameters at the command line
		/// </summary>
		/// <param name="strParameterList">Parameter list</param>
		/// <returns>True if any of the parameters are not present in strParameterList()</returns>
		public bool InvalidParametersPresent(string[] strParameterList)
		{
			const bool blnCaseSensitive = false;
			return InvalidParametersPresent(strParameterList, blnCaseSensitive);
		}

		/// <summary>
		/// Compares the parameter names in strParameterList with the parameters at the command line
		/// </summary>
		/// <param name="strParameterList">Parameter list</param>
		/// <param name="blnCaseSensitive">True to perform case-sensitive matching of the parameter name</param>
		/// <returns>True if any of the parameters are not present in strParameterList()</returns>
		public bool InvalidParametersPresent(IEnumerable<string> strParameterList, bool blnCaseSensitive)
		{
			if (InvalidParameters(strParameterList.ToList()).Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool InvalidParametersPresent(List<string> lstValidParameters, bool blnCaseSensitive)
		{

			if (InvalidParameters(lstValidParameters, blnCaseSensitive).Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}

		}

		public List<string> InvalidParameters(List<string> lstValidParameters)
		{
			const bool blnCaseSensitive = false;
			return InvalidParameters(lstValidParameters, blnCaseSensitive);
		}

		public List<string> InvalidParameters(List<string> lstValidParameters, bool blnCaseSensitive)
		{
			var lstInvalidParameters = new List<string>();

		
			try {
				// Find items in mSwitches whose keys are not in lstValidParameters)		

				foreach (var item in mSwitches) {
					var itemKey = item.Key;
					int intMatchCount;

					if (blnCaseSensitive) {
						intMatchCount = (from validItem in lstValidParameters 
                                         where validItem == itemKey 
                                         select validItem).Count();
					} else {
						intMatchCount = (from validItem in lstValidParameters 
                                         where string.Equals(validItem, itemKey, StringComparison.CurrentCultureIgnoreCase) 
                                         select validItem).Count();
					}

					if (intMatchCount == 0) {
						lstInvalidParameters.Add(item.Key);
					}
				}

			} catch (System.Exception ex) {
				throw new System.Exception("Error in InvalidParameters", ex);
			}

			return lstInvalidParameters;

		}

		/// <summary>
		/// Look for parameter on the command line
		/// </summary>
		/// <param name="strParameterName">Parameter name</param>
		/// <returns>True if present, otherwise false</returns>
		public bool IsParameterPresent(string strParameterName)
		{
			string strValue;
			const bool blnCaseSensitive = false;
			return RetrieveValueForParameter(strParameterName, out strValue, blnCaseSensitive);
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
		public bool ParseCommandLine(char chSwitchStartChar)
		{
			return ParseCommandLine(chSwitchStartChar, DEFAULT_SWITCH_PARAM_CHAR);
		}

		/// <summary>
		/// Parse the parameters and switches at the command line
		/// </summary>
		/// <param name="chSwitchStartChar"></param>
		/// <param name="chSwitchParameterChar"></param>
		/// <returns>Returns True if any command line parameters were found; otherwise false</returns>
		/// <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
		public bool ParseCommandLine(char chSwitchStartChar, char chSwitchParameterChar)
		{
			// Returns True if any command line parameters were found
			// Otherwise, returns false
			//
			// If /? or /help is found, then returns False and sets mShowHelp to True

			mSwitches.Clear();
			mNonSwitchParameters.Clear();

			try
			{
				string strCmdLine;
				try
				{
					// .CommandLine() returns the full command line
					strCmdLine = System.Environment.CommandLine;

					// .GetCommandLineArgs splits the command line at spaces, though it keeps text between double quotes together
					// Note that .NET will strip out the starting and ending double quote if the user provides a parameter like this:
					// MyProgram.exe "C:\Program Files\FileToProcess"
					//
					// In this case, strParameters(1) will not have a double quote at the start but it will have a double quote at the end:
					//  strParameters(1) = C:\Program Files\FileToProcess"

					// One very odd feature of System.Environment.GetCommandLineArgs() is that if the command line looks like this:
					//    MyProgram.exe "D:\My Folder\Subfolder\" /O:D:\OutputFolder
					// Then strParameters will have:
					//    strParameters(1) = D:\My Folder\Subfolder" /O:D:\OutputFolder
					//
					// To avoid this problem instead specify the command line as:
					//    MyProgram.exe "D:\My Folder\Subfolder" /O:D:\OutputFolder
					// which gives:
					//    strParameters(1) = D:\My Folder\Subfolder
					//    strParameters(2) = /O:D:\OutputFolder
					//
					// Due to the idiosyncrasies of .GetCommandLineArgs, we will instead use SplitCommandLineParams to do the splitting
					// strParameters = System.Environment.GetCommandLineArgs()

				}
				catch (System.Exception ex)
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

				var strParameters = SplitCommandLineParams(strCmdLine);

				if (mDebugMode)
				{
					Console.WriteLine();
				}

				if (string.IsNullOrWhiteSpace(strCmdLine))
				{
					return false;
				}
				
				if (strCmdLine.IndexOf(chSwitchStartChar + "?", StringComparison.Ordinal) > 0 || 
                    strCmdLine.ToLower().IndexOf(chSwitchStartChar + "help", StringComparison.CurrentCultureIgnoreCase) > 0)
				{
					mShowHelp = true;
					return false;
				}

				// Parse the command line
				// Note that strParameters(0) is the path to the Executable for the calling program

				for (var intIndex = 1; intIndex <= strParameters.Length - 1; intIndex++)
				{
				    if (strParameters[intIndex].Length <= 0)
				    {
				        continue;
				    }

				    var strKey = strParameters[intIndex].TrimStart(' ');
				    var strValue = string.Empty;

				    bool blnSwitchParam;
				    if (strKey.StartsWith(chSwitchStartChar))
				    {
				        blnSwitchParam = true;
				    }
				    else if (strKey.StartsWith(ALTERNATE_SWITCH_CHAR) || strKey.StartsWith(DEFAULT_SWITCH_CHAR))
				    {
				        blnSwitchParam = true;
				    }
				    else
				    {
				        // Parameter doesn't start with strSwitchStartChar or / or -
				        blnSwitchParam = false;
				    }

				    if (blnSwitchParam)
				    {
				        // Look for strSwitchParameterChar in strParameters[intIndex]
				        var intCharLoc = strParameters[intIndex].IndexOf(chSwitchParameterChar);

				        if (intCharLoc >= 0)
				        {
				            // Parameter is of the form /I:MyParam or /I:"My Parameter" or -I:"My Parameter" or /MyParam:Setting
				            strValue = strKey.Substring(intCharLoc + 1).Trim();

				            // Remove any starting and ending quotation marks
				            strValue = strValue.Trim('"');

				            strKey = strKey.Substring(0, intCharLoc);
				        }
				        else
				        {
				            // Parameter is of the form /S or -S
				        }

				        // Remove the switch character from strKey
				        strKey = strKey.Substring(1).Trim();

				        if (mDebugMode)
				        {
				            Console.WriteLine(@"SwitchParam: " + strKey + @"=" + strValue);
				        }

				        // Note: This will add strKey if it doesn't exist (which is normally the case)
				        mSwitches[strKey] = strValue;
				    }
				    else
				    {
				        // Non-switch parameter since strSwitchParameterChar was not found and does not start with strSwitchStartChar

				        // Remove any starting and ending quotation marks
				        strKey = strKey.Trim('"');

				        if (mDebugMode)
				        {
				            Console.WriteLine(@"NonSwitchParam " + mNonSwitchParameters.Count + @": " + strKey);
				        }

				        mNonSwitchParameters.Add(strKey);
				    }
				}

			}
			catch (System.Exception ex)
			{
				throw new System.Exception("Error in ParseCommandLine", ex);
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


		public static void PauseAtConsole(int intMillisecondsToPause, int intMillisecondsBetweenDots)
		{
			int intTotalIterations;

			Console.WriteLine();
			Console.Write(@"Continuing in " + (intMillisecondsToPause / 1000.0).ToString("0") + @" seconds ");

			try
			{
				if (intMillisecondsBetweenDots == 0)
					intMillisecondsBetweenDots = intMillisecondsToPause;

				intTotalIterations = (int)Math.Round(intMillisecondsToPause / (double)intMillisecondsBetweenDots, 0);
			}
			catch
			{
				// Ignore errors here
				intTotalIterations = 1;
			}

			var intIteration = 0;
			do
			{
				Console.Write('.');

				System.Threading.Thread.Sleep(intMillisecondsBetweenDots);

				intIteration += 1;
			} while (intIteration < intTotalIterations);

			Console.WriteLine();

		}

		/// <summary>
		/// Returns the value of the non-switch parameter at the given index
		/// </summary>
		/// <param name="intParameterIndex">Parameter index</param>
		/// <returns>The value of the parameter at the given index; empty string if no value or invalid index</returns>
		public string RetrieveNonSwitchParameter(int intParameterIndex)
		{
			var strValue = string.Empty;

			if (intParameterIndex < mNonSwitchParameters.Count)
			{
				strValue = mNonSwitchParameters[intParameterIndex];
			}

			if (string.IsNullOrEmpty(strValue))
			{
				return string.Empty;
			}

			return strValue;

		}

		/// <summary>
		/// Returns the parameter at the given index
		/// </summary>
		/// <param name="intParameterIndex">Parameter index</param>
		/// <param name="strKey">Parameter name (output)</param>
		/// <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
		/// <returns></returns>
		public bool RetrieveParameter(int intParameterIndex, out string strKey, out string strValue)
		{
			try
			{
				strKey = string.Empty;
				strValue = string.Empty;

				if (intParameterIndex < mSwitches.Count)
				{
					var iEnum = mSwitches.GetEnumerator();

					var intIndex = 0;
					while (iEnum.MoveNext())
					{
						if (intIndex == intParameterIndex)
						{
							strKey = iEnum.Current.Key;
							strValue = iEnum.Current.Value;
							return true;
						}
						intIndex += 1;
					}
				}
				else
				{
					return false;
				}
			}
			catch (System.Exception ex)
			{
				throw new System.Exception("Error in RetrieveParameter", ex);
			}

			return false;

		}

		/// <summary>
		/// Look for parameter on the command line and returns its value in strValue
		/// </summary>
		/// <param name="strKey">Parameter name</param>
		/// <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
		/// <returns>True if present, otherwise false</returns>
		public bool RetrieveValueForParameter(string strKey, out string strValue)
		{
			return RetrieveValueForParameter(strKey, out strValue, false);
		}

		/// <summary>
		/// Look for parameter on the command line and returns its value in strValue
		/// </summary>
		/// <param name="strKey">Parameter name</param>
		/// <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
		/// <param name="blnCaseSensitive">True to perform case-sensitive matching of the parameter name</param>
		/// <returns>True if present, otherwise false</returns>
		public bool RetrieveValueForParameter(string strKey, out string strValue, bool blnCaseSensitive)
		{

			try
			{
				strValue = string.Empty;

				if (blnCaseSensitive)
				{
				    if (mSwitches.ContainsKey(strKey))
					{
						strValue = Convert.ToString(mSwitches[strKey]);
						return true;
					}
				    
                    return false;
				}

                var result = (from item in mSwitches
                              where string.Equals(item.Key, strKey, StringComparison.CurrentCultureIgnoreCase)
                              select item).ToList();
			    
                if (result.Count > 0)
			    {
			        strValue = result.First().Value;
			        return true;
			    }

			    return false;
			}
			catch (System.Exception ex)
			{
				throw new System.Exception("Error in RetrieveValueForParameter", ex);
			}

		}

		private string[] SplitCommandLineParams(string strCmdLine)
		{
			var strParameters = new List<string>();

			var intIndexStart = 0;
			var intIndexEnd = 0;

			try
			{

				if (!string.IsNullOrEmpty(strCmdLine))
				{
					// Make sure the command line doesn't have any carriage return or linefeed characters
					strCmdLine = strCmdLine.Replace("\r", " ");
					strCmdLine = strCmdLine.Replace("\n", " ");					
				
					var blnInsideDoubleQuotes = false;

					while (intIndexStart < strCmdLine.Length)
					{
						// Step through the characters to find the next space
						// However, if we find a double quote, then stop checking for spaces

						if (strCmdLine[intIndexEnd] == '"')
						{
							blnInsideDoubleQuotes = !blnInsideDoubleQuotes;
						}

						if (!blnInsideDoubleQuotes || intIndexEnd == strCmdLine.Length - 1)
						{
							if (strCmdLine[intIndexEnd] == ' ' || intIndexEnd == strCmdLine.Length - 1)
							{
								// Found the end of a parameter
								var strParameter = strCmdLine.Substring(intIndexStart, intIndexEnd - intIndexStart + 1).TrimEnd(' ');

								if (strParameter.StartsWith('"'))
								{
									strParameter = strParameter.Substring(1);
								}

								if (strParameter.EndsWith('"'))
								{
									strParameter = strParameter.Substring(0, strParameter.Length - 1);
								}

								if (!string.IsNullOrEmpty(strParameter))
								{
									if (mDebugMode)
									{
										Console.WriteLine(@"Param " + strParameters.Count + @": " + strParameter);
									}
									strParameters.Add(strParameter);
								}

								intIndexStart = intIndexEnd + 1;
							}
						}

						intIndexEnd += 1;
					}
				}
			}
			catch (System.Exception ex)
			{
				throw new System.Exception("Error in SplitCommandLineParams", ex);
			}

			return strParameters.ToArray();

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