Option Strict On

' This class can be used to parse the text following the program name when a 
'  program is started from the command line
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started November 8, 2003

' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute, 
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
' Department of Energy (DOE).  All rights in the computer software are reserved 
' by DOE on behalf of the United States Government and the Contractor as 
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
' SOFTWARE.  This notice including this sentence must appear on any copies of 
' this computer software.

'
' Last modified October 29, 2015

Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class clsParseCommandLine

    Public Const DEFAULT_SWITCH_CHAR As Char = "/"c
    Public Const ALTERNATE_SWITCH_CHAR As Char = "-"c

    Public Const DEFAULT_SWITCH_PARAM_CHAR As Char = ":"c

    Private ReadOnly mSwitches As New Dictionary(Of String, String)
    Private ReadOnly mNonSwitchParameters As New List(Of String)

    Private mShowHelp As Boolean = False
    Private mDebugMode As Boolean = False

    Public ReadOnly Property NeedToShowHelp() As Boolean
        Get
            Return mShowHelp
        End Get
    End Property

    Public ReadOnly Property ParameterCount() As Integer
        Get
            Return mSwitches.Count
        End Get
    End Property

    Public ReadOnly Property NonSwitchParameterCount() As Integer
        Get
            Return mNonSwitchParameters.Count
        End Get
    End Property

    Public Property DebugMode() As Boolean
        Get
            Return mDebugMode
        End Get
        Set(value As Boolean)
            mDebugMode = value
        End Set
    End Property

    ''' <summary>
    ''' Compares the parameter names in objParameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="objParameterList">Parameter list</param>
    ''' <returns>True if any of the parameters are not present in strParameterList()</returns>
    Public Function InvalidParametersPresent(objParameterList As List(Of String)) As Boolean
        Const blnCaseSensitive As Boolean = False
        Return InvalidParametersPresent(objParameterList, blnCaseSensitive)
    End Function

    ''' <summary>
    ''' Compares the parameter names in strParameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="strParameterList">Parameter list</param>
    ''' <returns>True if any of the parameters are not present in strParameterList()</returns>
    Public Function InvalidParametersPresent(strParameterList() As String) As Boolean
        Const blnCaseSensitive As Boolean = False
        Return InvalidParametersPresent(strParameterList, blnCaseSensitive)
    End Function

    ''' <summary>
    ''' Compares the parameter names in strParameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="strParameterList">Parameter list</param>
    ''' <param name="blnCaseSensitive">True to perform case-sensitive matching of the parameter name</param>
    ''' <returns>True if any of the parameters are not present in strParameterList()</returns>
    Public Function InvalidParametersPresent(strParameterList() As String, blnCaseSensitive As Boolean) As Boolean
        If InvalidParameters(strParameterList.ToList()).Count > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function InvalidParametersPresent(lstValidParameters As List(Of String), blnCaseSensitive As Boolean) As Boolean

        If InvalidParameters(lstValidParameters, blnCaseSensitive).Count > 0 Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function InvalidParameters(lstValidParameters As List(Of String)) As List(Of String)
        Const blnCaseSensitive As Boolean = False
        Return InvalidParameters(lstValidParameters, blnCaseSensitive)
    End Function

    Public Function InvalidParameters(lstValidParameters As List(Of String), blnCaseSensitive As Boolean) As List(Of String)
        Dim lstInvalidParameters = New List(Of String)

        Try

            ' Find items in mSwitches whose keys are not in lstValidParameters)		
            For Each item As KeyValuePair(Of String, String) In mSwitches

                Dim itemKey As String = item.Key
                Dim intMatchCount As Integer

                If blnCaseSensitive Then
                    intMatchCount = (From validItem In lstValidParameters Where validItem = itemKey).Count
                Else
                    intMatchCount = (From validItem In lstValidParameters Where String.Equals(validItem, itemKey, StringComparison.InvariantCultureIgnoreCase)).Count
                End If

                If intMatchCount = 0 Then
                    lstInvalidParameters.Add(item.Key)
                End If
            Next

        Catch ex As Exception
            Throw New Exception("Error in InvalidParameters", ex)
        End Try

        Return lstInvalidParameters

    End Function

    ''' <summary>
    ''' Look for parameter on the command line
    ''' </summary>
    ''' <param name="strParameterName">Parameter name</param>
    ''' <returns>True if present, otherwise false</returns>
    ''' <remarks>Does not work for /? or /help -- for those, use .NeedToShowHelp</remarks>
    Public Function IsParameterPresent(strParameterName As String) As Boolean
        Dim strValue As String = String.Empty
        Const blnCaseSensitive = False
        Return RetrieveValueForParameter(strParameterName, strValue, blnCaseSensitive)
    End Function

    ''' <summary>
    ''' Parse the parameters and switches at the command line; uses / for the switch character and : for the switch parameter character
    ''' </summary>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine() As Boolean
        Return ParseCommandLine(DEFAULT_SWITCH_CHAR, DEFAULT_SWITCH_PARAM_CHAR)
    End Function

    ''' <summary>
    ''' Parse the parameters and switches at the command line; uses : for the switch parameter character
    ''' </summary>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine(strSwitchStartChar As Char) As Boolean
        Return ParseCommandLine(strSwitchStartChar, DEFAULT_SWITCH_PARAM_CHAR)
    End Function

    ''' <summary>
    ''' Parse the parameters and switches at the command line
    ''' </summary>
    ''' <param name="chSwitchStartChar"></param>
    ''' <param name="chSwitchParameterChar"></param>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine(chSwitchStartChar As Char, chSwitchParameterChar As Char) As Boolean
        ' Returns True if any command line parameters were found
        ' Otherwise, returns false
        '
        ' If /? or /help is found, then returns False and sets mShowHelp to True

        Dim strCmdLine As String = String.Empty

        mSwitches.Clear()
        mNonSwitchParameters.Clear()

        Try
            Try
                ' .CommandLine() returns the full command line
                strCmdLine = Environment.CommandLine()

                ' .GetCommandLineArgs splits the command line at spaces, though it keeps text between double quotes together
                ' Note that .NET will strip out the starting and ending double quote if the user provides a parameter like this:
                ' MyProgram.exe "C:\Program Files\FileToProcess"
                '
                ' In this case, strParameters(1) will not have a double quote at the start but it will have a double quote at the end:
                '  strParameters(1) = C:\Program Files\FileToProcess"

                ' One very odd feature of Environment.GetCommandLineArgs() is that if the command line looks like this:
                '    MyProgram.exe "D:\My Folder\Subfolder\" /O:D:\OutputFolder
                ' Then strParameters will have:
                '    strParameters(1) = D:\My Folder\Subfolder" /O:D:\OutputFolder
                '
                ' To avoid this problem instead specify the command line as:
                '    MyProgram.exe "D:\My Folder\Subfolder" /O:D:\OutputFolder
                ' which gives:
                '    strParameters(1) = D:\My Folder\Subfolder
                '    strParameters(2) = /O:D:\OutputFolder
                '
                ' Due to the idiosyncrasies of .GetCommandLineArgs, we will instead use SplitCommandLineParams to do the splitting
                ' strParameters = Environment.GetCommandLineArgs()

            Catch ex As Exception
                ' In .NET 1.x, programs would fail if called from a network share
                ' This appears to be fixed in .NET 2.0 and above
                ' If an exception does occur here, we'll show the error message at the console, then sleep for 2 seconds

                Console.WriteLine("------------------------------------------------------------------------------")
                Console.WriteLine("This program cannot be run from a network share.  Please map a drive to the")
                Console.WriteLine(" network share you are currently accessing or copy the program files and")
                Console.WriteLine(" required DLL's to your local computer.")
                Console.WriteLine(" Exception: " & ex.Message)
                Console.WriteLine("------------------------------------------------------------------------------")

                PauseAtConsole(5000, 1000)

                mShowHelp = True
                Return False
            End Try

            If mDebugMode Then
                Console.WriteLine()
                Console.WriteLine("Debugging command line parsing")
                Console.WriteLine()
            End If

            Dim strParameters = SplitCommandLineParams(strCmdLine)

            If mDebugMode Then
                Console.WriteLine()
            End If

            If String.IsNullOrWhiteSpace(strCmdLine) Then
                Return False
            ElseIf strCmdLine.IndexOf(chSwitchStartChar & "?", StringComparison.Ordinal) > 0 OrElse strCmdLine.ToLower().IndexOf(chSwitchStartChar & "help", StringComparison.Ordinal) > 0 Then
                mShowHelp = True
                Return False
            End If

            ' Parse the command line
            ' Note that strParameters(0) is the path to the Executable for the calling program
            For intIndex = 1 To strParameters.Length - 1

                If strParameters(intIndex).Length > 0 Then
                    Dim strKey = strParameters(intIndex).TrimStart(" "c)
                    Dim strValue = String.Empty
                    Dim blnSwitchParam As Boolean

                    If strKey.StartsWith(chSwitchStartChar) Then
                        blnSwitchParam = True
                    ElseIf strKey.StartsWith(ALTERNATE_SWITCH_CHAR) OrElse strKey.StartsWith(DEFAULT_SWITCH_CHAR) Then
                        blnSwitchParam = True
                    Else
                        ' Parameter doesn't start with strSwitchStartChar or / or -
                        blnSwitchParam = False
                    End If

                    If blnSwitchParam Then
                        ' Look for strSwitchParameterChar in strParameters(intIndex)
                        Dim intCharIndex = strParameters(intIndex).IndexOf(chSwitchParameterChar)

                        If intCharIndex >= 0 Then
                            ' Parameter is of the form /I:MyParam or /I:"My Parameter" or -I:"My Parameter" or /MyParam:Setting
                            strValue = strKey.Substring(intCharIndex + 1).Trim

                            ' Remove any starting and ending quotation marks
                            strValue = strValue.Trim(""""c)

                            strKey = strKey.Substring(0, intCharIndex)
                        Else
                            ' Parameter is of the form /S or -S
                        End If

                        ' Remove the switch character from strKey
                        strKey = strKey.Substring(1).Trim

                        If mDebugMode Then
                            Console.WriteLine("SwitchParam: " & strKey & "=" & strValue)
                        End If

                        ' Note: .Item() will add strKey if it doesn't exist (which is normally the case)
                        mSwitches.Item(strKey) = strValue
                    Else
                        ' Non-switch parameter since strSwitchParameterChar was not found and does not start with strSwitchStartChar

                        ' Remove any starting and ending quotation marks
                        strKey = strKey.Trim(""""c)

                        If mDebugMode Then
                            Console.WriteLine("NonSwitchParam " & mNonSwitchParameters.Count & ": " & strKey)
                        End If

                        mNonSwitchParameters.Add(strKey)
                    End If

                End If
            Next intIndex

        Catch ex As Exception
            Throw New Exception("Error in ParseCommandLine", ex)
        End Try

        If mDebugMode Then
            Console.WriteLine()
            Console.WriteLine("Switch Count = " & mSwitches.Count)
            Console.WriteLine("NonSwitch Count = " & mNonSwitchParameters.Count)
            Console.WriteLine()
        End If

        If mSwitches.Count + mNonSwitchParameters.Count > 0 Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Shared Sub PauseAtConsole(intMillisecondsToPause As Integer, intMillisecondsBetweenDots As Integer)

        Dim intIteration As Integer
        Dim intTotalIterations As Integer

        Console.WriteLine()
        Console.Write("Continuing in " & (intMillisecondsToPause / 1000.0).ToString("0") & " seconds ")

        Try
            If intMillisecondsBetweenDots = 0 Then intMillisecondsBetweenDots = intMillisecondsToPause

            intTotalIterations = CInt(Math.Round(intMillisecondsToPause / intMillisecondsBetweenDots, 0))
        Catch ex As Exception
            intTotalIterations = 1
        End Try

        intIteration = 0
        Do
            Console.Write("."c)

            System.Threading.Thread.Sleep(intMillisecondsBetweenDots)

            intIteration += 1
        Loop While intIteration < intTotalIterations

        Console.WriteLine()

    End Sub

    ''' <summary>
    ''' Returns the value of the non-switch parameter at the given index
    ''' </summary>
    ''' <param name="intParameterIndex">Parameter index</param>
    ''' <returns>The value of the parameter at the given index; empty string if no value or invalid index</returns>
    Public Function RetrieveNonSwitchParameter(intParameterIndex As Integer) As String
        Dim strValue As String = String.Empty

        If intParameterIndex < mNonSwitchParameters.Count Then
            strValue = mNonSwitchParameters(intParameterIndex)
        End If

        If strValue Is Nothing Then
            strValue = String.Empty
        End If

        Return strValue

    End Function

    ''' <summary>
    ''' Returns the parameter at the given index
    ''' </summary>
    ''' <param name="intParameterIndex">Parameter index</param>
    ''' <param name="strKey">Parameter name (output)</param>
    ''' <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <returns></returns>
    Public Function RetrieveParameter(intParameterIndex As Integer, <Out()> ByRef strKey As String, <Out()> ByRef strValue As String) As Boolean

        Dim intIndex As Integer

        Try
            strKey = String.Empty
            strValue = String.Empty

            If intParameterIndex < mSwitches.Count Then
                Dim iEnum As Dictionary(Of String, String).Enumerator = mSwitches.GetEnumerator()

                intIndex = 0
                Do While iEnum.MoveNext()
                    If intIndex = intParameterIndex Then
                        strKey = iEnum.Current.Key
                        strValue = iEnum.Current.Value
                        Return True
                    End If
                    intIndex += 1
                Loop
            Else
                Return False
            End If
        Catch ex As Exception
            Throw New Exception("Error in RetrieveParameter", ex)
        End Try

        Return False

    End Function

    ''' <summary>
    ''' Look for parameter on the command line and returns its value in strValue
    ''' </summary>
    ''' <param name="strKey">Parameter name</param>
    ''' <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <returns>True if present, otherwise false</returns>
    Public Function RetrieveValueForParameter(strKey As String, <Out()> ByRef strValue As String) As Boolean
        Return RetrieveValueForParameter(strKey, strValue, False)
    End Function

    ''' <summary>
    ''' Look for parameter on the command line and returns its value in strValue
    ''' </summary>
    ''' <param name="strKey">Parameter name</param>
    ''' <param name="strValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <param name="blnCaseSensitive">True to perform case-sensitive matching of the parameter name</param>
    ''' <returns>True if present, otherwise false</returns>
    Public Function RetrieveValueForParameter(strKey As String, <Out()> ByRef strValue As String, blnCaseSensitive As Boolean) As Boolean

        Try
            strValue = String.Empty

            If blnCaseSensitive Then
                If mSwitches.ContainsKey(strKey) Then
                    strValue = CStr(mSwitches(strKey))
                    Return True
                Else
                    Return False
                End If
            Else
                Dim query = (From item In mSwitches Where String.Equals(item.Key, strKey, StringComparison.InvariantCultureIgnoreCase) Select item).ToList()

                If query.Count = 0 Then
                    Return False
                End If

                strValue = query.FirstOrDefault.Value
                Return True

            End If
        Catch ex As Exception
            Throw New Exception("Error in RetrieveValueForParameter", ex)
        End Try

    End Function

    Private Function SplitCommandLineParams(strCmdLine As String) As String()
        Dim strParameters As New List(Of String)
        Dim strParameter As String

        Dim intIndexStart = 0
        Dim intIndexEnd = 0
        Dim blnInsideDoubleQuotes As Boolean

        Try
            If Not String.IsNullOrEmpty(strCmdLine) Then

                ' Make sure the command line doesn't have any carriage return or linefeed characters
                strCmdLine = strCmdLine.Replace(ControlChars.CrLf, " ")
                strCmdLine = strCmdLine.Replace(ControlChars.Cr, " ")
                strCmdLine = strCmdLine.Replace(ControlChars.Lf, " ")

                blnInsideDoubleQuotes = False

                Do While intIndexStart < strCmdLine.Length
                    ' Step through the characters to find the next space
                    ' However, if we find a double quote, then stop checking for spaces

                    If strCmdLine.Chars(intIndexEnd) = """"c Then
                        blnInsideDoubleQuotes = Not blnInsideDoubleQuotes
                    End If

                    If Not blnInsideDoubleQuotes OrElse intIndexEnd = strCmdLine.Length - 1 Then
                        If strCmdLine.Chars(intIndexEnd) = " "c OrElse intIndexEnd = strCmdLine.Length - 1 Then
                            ' Found the end of a parameter
                            strParameter = strCmdLine.Substring(intIndexStart, intIndexEnd - intIndexStart + 1).TrimEnd(" "c)

                            If strParameter.StartsWith(""""c) Then
                                strParameter = strParameter.Substring(1)
                            End If

                            If strParameter.EndsWith(""""c) Then
                                strParameter = strParameter.Substring(0, strParameter.Length - 1)
                            End If

                            If Not String.IsNullOrEmpty(strParameter) Then
                                If mDebugMode Then
                                    Console.WriteLine("Param " & strParameters.Count & ": " & strParameter)
                                End If
                                strParameters.Add(strParameter)
                            End If

                            intIndexStart = intIndexEnd + 1
                        End If
                    End If

                    intIndexEnd += 1
                Loop

            End If

        Catch ex As Exception
            Throw New Exception("Error in SplitCommandLineParams", ex)
        End Try

        Return strParameters.ToArray()

    End Function
End Class
