Option Strict On

' These functions utilize MSFileReader.XRawfile2.dll to extract scan header info and
' raw mass spectrum info from Finnigan LCQ, LTQ, and LTQ-FT files
' 
' Required Dlls: fileio.dll, fregistry.dll, and MSFileReader.XRawfile2.dll
' DLLs obtained from: Thermo software named "MSFileReader2.2"
' Download link: http://sjsupport.thermofinnigan.com/public/detail.asp?id=703
'
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
'
' Switched from XRawFile2.dll to MSFileReader.XRawfile2.dll in March 2012 (that DLL comes with ProteoWizard)
'
' If having troubles reading files, install MS File Reader 3.0 SP3
' Download link: https://thermo.flexnetoperations.com/control/thmo/login
' Last modified October 13, 2015

Imports System.Runtime.InteropServices
Imports System.Linq
Imports MSFileReaderLib
Imports System.Text.RegularExpressions


Namespace FinniganFileIO

    Public Class XRawFileIO
        Inherits FinniganFileReaderBaseClass

#Region "Constants and Enums"

        ' Note that each of these strings has a space at the end; this is important to avoid matching inappropriate text in the filter string
        Private Const MS_ONLY_C_TEXT As String = " c ms "
        Private Const MS_ONLY_P_TEXT As String = " p ms "

        Private Const MS_ONLY_PZ_TEXT As String = " p Z ms "            ' Likely a zoom scan
        Private Const MS_ONLY_DZ_TEXT As String = " d Z ms "            ' Dependent zoom scan
        Private Const MS_ONLY_PZ_MS2_TEXT As String = " d Z ms2 "       ' Dependent MS2 zoom scan
        Private Const MS_ONLY_Z_TEXT As String = " NSI Z ms "           ' Likely a zoom scan

        Private Const FULL_MS_TEXT As String = "Full ms "
        Private Const FULL_PR_TEXT As String = "Full pr "               ' TSQ: Full Parent Scan, Product Mass
        Private Const SIM_MS_TEXT As String = "SIM ms "
        Private Const FULL_LOCK_MS_TEXT As String = "Full lock ms "     ' Lock mass scan

        Private Const MRM_Q1MS_TEXT As String = "Q1MS "
        Private Const MRM_Q3MS_TEXT As String = "Q3MS "
        Private Const MRM_SRM_TEXT As String = "SRM ms2"
        Private Const MRM_FullNL_TEXT As String = "Full cnl "           ' MRM neutral loss
        Private Const MRM_SIM_PR_TEXT As String = "SIM pr "             ' TSQ: Isolated and fragmented parent, monitor multiple product ion ranges; e.g., Biofilm-1000pg-std-mix_06Dec14_Smeagol-3

        ' This RegEx matches Full ms2, Full ms3, ..., Full ms10, Full ms11, ...
        ' It also matches p ms2
        ' It also matches SRM ms2
        ' It also matches CRM ms3
        ' It also matches Full msx ms2 (multiplexed parent ion selection, introduced with the Q-Exactive)
        Private Const MS2_REGEX As String = "( p|Full|SRM|CRM|Full msx) ms([2-9]|[1-9][0-9]) "

        ' Used with .GetSeqRowSampleType()
        Public Enum SampleTypeConstants
            Unknown = 0
            Blank = 1
            QC = 2
            StandardClear_None = 3
            StandardUpdate_None = 4
            StandardBracket_Open = 5
            StandardBracketStart_MultipleBrackets = 6
            StandardBracketEnd_multipleBrackets = 7
        End Enum

        ' Used with .SetController()
        Public Enum ControllerTypeConstants
            NoDevice = -1
            MS = 0
            Analog = 1
            AD_Card = 2
            PDA = 3
            UV = 4
        End Enum

        ' Used with .GetMassListXYZ()
        Public Enum IntensityCutoffTypeConstants
            None = 0                        ' AllValuesReturned
            AbsoluteIntensityUnits = 1
            RelativeToBasePeak = 2
        End Enum

        'Public Enum ErrorCodeConstants
        '    MassRangeFormatIncorrect = -6
        '    FilterFormatIncorrect = -5
        '    ParameterInvalid = -4
        '    OperationNotSupportedOnCurrentController = -3
        '    CurrentControllerInvalid = -2
        '    RawFileInvalid = -1
        '    Failed = 0
        '    Success = 1
        '    NoDataPresent = 2
        'End Enum

        Public Class InstFlags
            Public Const TIM As String = "Total Ion Map"
            Public Const NLM As String = "Neutral Loss Map"
            Public Const PIM As String = "Parent Ion Map"
            Public Const DDZMap As String = "Data Dependent ZoomScan Map"
        End Class

#End Region

#Region "Structures"
        Public Structure udtParentIonInfoType
            Public MSLevel As Integer
            Public ParentIonMZ As Double
            Public CollisionMode As String
            Public CollisionMode2 As String
            Public CollisionEnergy As Single
            Public CollisionEnergy2 As Single
            Public ActivationType As ActivationTypeConstants
            Public Sub Clear()
                MSLevel = 1
                ParentIonMZ = 0
                CollisionMode = String.Empty
                CollisionMode2 = String.Empty
                CollisionEnergy = 0
                CollisionEnergy2 = 0
                ActivationType = ActivationTypeConstants.Unknown
            End Sub

            Public Overrides Function ToString() As String
                If String.IsNullOrWhiteSpace(CollisionMode) Then
                    Return "ms" & MSLevel & " " & ParentIonMZ.ToString("0.0#")
                Else
                    Return "ms" & MSLevel & " " & ParentIonMZ.ToString("0.0#") & "@" & CollisionMode & CollisionEnergy.ToString("0.00")
                End If
            End Function

        End Structure

        Public Structure udtMassPrecisionInfoType
            Public Intensity As Double
            Public Mass As Double
            Public AccuracyMMU As Double
            Public AccuracyPPM As Double
            Public Resolution As Double
        End Structure

        Public Structure udtFTLabelInfoType
            Public Mass As Double
            Public Intensity As Double
            Public Resolution As Single
            Public Baseline As Single
            Public Noise As Single
            Public Charge As Integer
        End Structure

        '' Only used by GetNoiseData(), which is commented out.
        'Public Structure udtNoisePackets
        '    Public Mass As Double
        '    Public Noise As Single
        '    Public Baseline As Single
        'End Structure
#End Region

#Region "Classwide Variables"

        ' Cached XRawFile object, for faster accessing
        Private mXRawFile As IXRawfile5

        Private mCorruptMemoryEncountered As Boolean

#End Region

        Private Sub CacheScanInfo(ByVal scan As Integer, ByVal scanInfo As clsScanInfo)

            If mCachedScanInfo.Count > MAX_SCANS_TO_CACHE_INFO Then
                ' Remove the oldest entry in mCachedScanInfo

                Dim minimumScanNumber As Integer = -1
                Dim dtMinimumCacheDate = DateTime.UtcNow

                For Each cachedInfo In mCachedScanInfo.Values
                    If minimumScanNumber < 0 OrElse cachedInfo.CacheDateUTC < dtMinimumCacheDate Then
                        minimumScanNumber = cachedInfo.ScanNumber
                        dtMinimumCacheDate = cachedInfo.CacheDateUTC
                    End If
                Next

                If mCachedScanInfo.ContainsKey(minimumScanNumber) Then
                    mCachedScanInfo.Remove(minimumScanNumber)
                End If
            End If

            If mCachedScanInfo.ContainsKey(scan) Then
                mCachedScanInfo.Remove(scan)
            End If

            mCachedScanInfo.Add(scan, scanInfo)

        End Sub

        Private Shared Function CapitalizeCollisionMode(strCollisionMode As String) As String

            If (String.Equals(strCollisionMode, "EThcD", StringComparison.InvariantCultureIgnoreCase)) Then
                Return "EThcD"
            End If

            If (String.Equals(strCollisionMode, "ETciD", StringComparison.InvariantCultureIgnoreCase)) Then
                Return "ETciD"
            End If

            Return strCollisionMode.ToUpper()

        End Function

        Public Overrides Function CheckFunctionality() As Boolean
            ' I have a feeling this doesn't actually work, and will always return True

            Try
                Dim objXRawFile As New MSFileReader_XRawfile
                objXRawFile = Nothing

                ' If we get here, then all is fine
                Return True
            Catch ex As Exception
                Return False
            End Try

        End Function

        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Overrides Sub CloseRawFile()

            Try
                If Not mXRawFile Is Nothing Then
                    mXRawFile.Close()
                End If
                mCorruptMemoryEncountered = False

            Catch ex As AccessViolationException
                ' Ignore this error
            Catch ex As Exception
                ' Ignore any errors
            Finally
                mXRawFile = Nothing
                mCachedFileName = String.Empty
            End Try

        End Sub

        Private Shared Function ContainsAny(stringToSearch As String, itemsToFind As List(Of String), Optional indexSearchStart As Integer = 0, Optional matchCase As Boolean = False) As Boolean

            Return itemsToFind.Any(Function(item) ContainsText(stringToSearch, item, indexSearchStart, matchCase))

        End Function

        Private Shared Function ContainsText(stringToSearch As String, textToFind As String, Optional indexSearchStart As Integer = 0, Optional matchCase As Boolean = False) As Boolean

            If matchCase Then
                If stringToSearch.IndexOf(textToFind, StringComparison.InvariantCulture) >= indexSearchStart Then
                    Return True
                End If
            Else
                If stringToSearch.IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) >= indexSearchStart Then
                    Return True
                End If
            End If

            Return False

        End Function

        Public Shared Function DetermineMRMScanType(ByVal strFilterText As String) As MRMScanTypeConstants
            Dim eMRMScanType = MRMScanTypeConstants.NotMRM

            If String.IsNullOrWhiteSpace(strFilterText) Then
                Return eMRMScanType
            End If

            Dim mrmQMSTags = New List(Of String) From {MRM_Q1MS_TEXT, MRM_Q3MS_TEXT}

            If ContainsAny(strFilterText, mrmQMSTags, 1) Then
                eMRMScanType = MRMScanTypeConstants.MRMQMS
            ElseIf ContainsText(strFilterText, MRM_SRM_TEXT, 1) Then
                eMRMScanType = MRMScanTypeConstants.SRM
            ElseIf ContainsText(strFilterText, MRM_SIM_PR_TEXT, 1) Then
                ' This is not technically SRM, but the data looks very similar, so we'll track it like SRM data
                eMRMScanType = MRMScanTypeConstants.SRM
            ElseIf ContainsText(strFilterText, MRM_FullNL_TEXT, 1) Then
                eMRMScanType = MRMScanTypeConstants.FullNL

            End If

            Return eMRMScanType
        End Function

        Public Shared Function DetermineIonizationMode(ByVal strFiltertext As String) As IonModeConstants

            ' Determine the ion mode by simply looking for the first + or - sign

            Const IONMODE_REGEX = "[+-]"

            Static reIonMode As Regex

            Dim eIonMode As IonModeConstants
            Dim reMatch As Match

            If reIonMode Is Nothing Then
                reIonMode = New Regex(IONMODE_REGEX, RegexOptions.Compiled)
            End If

            eIonMode = IonModeConstants.Unknown

            If Not String.IsNullOrWhiteSpace(strFiltertext) Then
                ' Parse out the text between the square brackets
                reMatch = reIonMode.Match(strFiltertext)

                If Not reMatch Is Nothing AndAlso reMatch.Success Then
                    Select Case reMatch.Value
                        Case "+"
                            eIonMode = IonModeConstants.Positive
                        Case "-"
                            eIonMode = IonModeConstants.Negative
                        Case Else
                            eIonMode = IonModeConstants.Unknown
                    End Select
                End If

            End If

            Return eIonMode

        End Function

        Public Shared Sub ExtractMRMMasses(
          ByVal strFilterText As String,
          ByVal eMRMScanType As MRMScanTypeConstants,
          <Out()> ByRef udtMRMInfo As udtMRMInfoType)

            ' Parse out the MRM_QMS or SRM mass info from strFilterText
            ' It should be of the form 
            ' MRM_Q1MS_TEXT:    p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]
            ' or
            ' MRM_Q3MS_TEXT:    p NSI Q3MS [150.070-1500.000]
            ' or
            ' MRM_SRM_TEXT:    c NSI SRM ms2 489.270@cid17.00 [397.209-392.211, 579.289-579.291]

            ' Note: we do not parse mass information out for Full Neutral Loss scans
            ' MRM_FullNL_TEXT: c NSI Full cnl 162.053 [300.000-1200.000]

            Const MASSLIST_REGEX = "\[[0-9.]+-[0-9.]+.*\]"
            Const MASSRANGES_REGEX = "([0-9.]+)-([0-9.]+)"

            Static reMassList As Regex
            Static reMassRanges As Regex

            Dim reMatch As Match

            If reMassList Is Nothing Then
                reMassList = New Regex(MASSLIST_REGEX, RegexOptions.Compiled)
            End If

            If reMassRanges Is Nothing Then
                reMassRanges = New Regex(MASSRANGES_REGEX, RegexOptions.Compiled)
            End If

            udtMRMInfo = New udtMRMInfoType
            If udtMRMInfo.MRMMassList Is Nothing Then
                InitializeMRMInfo(udtMRMInfo, 0)
            Else
                udtMRMInfo.MRMMassCount = 0
            End If

            If Not String.IsNullOrWhiteSpace(strFilterText) Then

                If eMRMScanType = MRMScanTypeConstants.MRMQMS Or
                   eMRMScanType = MRMScanTypeConstants.SRM Then

                    ' Parse out the text between the square brackets
                    reMatch = reMassList.Match(strFilterText)

                    If reMatch.Success Then
                        reMatch = reMassRanges.Match(reMatch.Value)
                        If Not reMatch Is Nothing Then

                            InitializeMRMInfo(udtMRMInfo, 2)

                            Do While reMatch.Success
                                Try
                                    ' Note that group 0 is the full mass range (two mass values, separated by a dash)
                                    ' Group 1 is the first mass value
                                    ' Group 2 is the second mass value

                                    If udtMRMInfo.MRMMassCount = udtMRMInfo.MRMMassList.Length Then
                                        ' Need to reserve more room
                                        ReDim Preserve udtMRMInfo.MRMMassList(udtMRMInfo.MRMMassList.Length * 2 - 1)
                                    End If

                                    With udtMRMInfo.MRMMassList(udtMRMInfo.MRMMassCount)
                                        .StartMass = Double.Parse(reMatch.Groups(1).Value)
                                        .EndMass = Double.Parse(reMatch.Groups(2).Value)
                                        .CentralMass = Math.Round(.StartMass + (.EndMass - .StartMass) / 2, 6)
                                    End With
                                    udtMRMInfo.MRMMassCount += 1

                                Catch ex As Exception
                                    ' Error parsing out the mass values; skip this group
                                End Try

                                reMatch = reMatch.NextMatch
                            Loop

                        End If
                    End If
                Else
                    ' Unsupported MRM type
                End If
            End If

            If udtMRMInfo.MRMMassList.Length > udtMRMInfo.MRMMassCount Then
                If udtMRMInfo.MRMMassCount <= 0 Then
                    ReDim udtMRMInfo.MRMMassList(-1)
                Else
                    ReDim Preserve udtMRMInfo.MRMMassList(udtMRMInfo.MRMMassCount - 1)
                End If
            End If

        End Sub

        ''' <summary>
        ''' Parse out the parent ion and collision energy from strFilterText
        ''' </summary>
        ''' <param name="strFilterText"></param>
        ''' <param name="dblParentIonMZ">Parent ion m/z (output)</param>
        ''' <param name="intMSLevel">MSLevel (output)</param>
        ''' <param name="strCollisionMode">Collision mode (output)</param>
        ''' <returns>True if success</returns>
        ''' <remarks>If multiple parent ion m/z values are listed then dblParentIonMZ will have the last one.  However, if the filter text contains "Full msx" then dblParentIonMZ will have the first parent ion listed</remarks>
        Public Shared Function ExtractParentIonMZFromFilterText(
          ByVal strFilterText As String,
          <Out()> ByRef dblParentIonMZ As Double,
          <Out()> ByRef intMSLevel As Integer,
          <Out()> ByRef strCollisionMode As String) As Boolean

            Dim lstParentIons As List(Of udtParentIonInfoType) = Nothing

            Return ExtractParentIonMZFromFilterText(strFilterText, dblParentIonMZ, intMSLevel, strCollisionMode, lstParentIons)

        End Function

        ''' <summary>
        ''' Parse out the parent ion and collision energy from strFilterText
        ''' </summary>
        ''' <param name="strFilterText"></param>
        ''' <param name="dblParentIonMZ">Parent ion m/z (output)</param>
        ''' <param name="intMSLevel">MSLevel (output)</param>
        ''' <param name="strCollisionMode">Collision mode (output)</param>
        ''' <returns>True if success</returns>
        ''' <remarks>If multiple parent ion m/z values are listed then dblParentIonMZ will have the last one.  However, if the filter text contains "Full msx" then dblParentIonMZ will have the first parent ion listed</remarks>
        Public Shared Function ExtractParentIonMZFromFilterText(
           ByVal strFilterText As String,
           <Out()> ByRef dblParentIonMZ As Double,
           <Out()> ByRef intMSLevel As Integer,
           <Out()> ByRef strCollisionMode As String,
           <Out()> ByRef lstParentIons As List(Of udtParentIonInfoType)) As Boolean

            ' strFilterText should be of the form "+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]"
            ' or "+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
            ' or "ITMS + c NSI d Full ms10 421.76@35.00"
            ' or "ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]"              ' Note: sa stands for "supplemental activation"
            ' or "ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]" 
            ' or "ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]"
            ' or "ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]"
            ' or "ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]"
            ' or "FTMS + p NSI Full ms [400.00-2000.00]"  (high res full MS)
            ' or "ITMS + c ESI Full ms [300.00-2000.00]"  (low res full MS)
            ' or "ITMS + p ESI d Z ms [1108.00-1118.00]"  (zoom scan)
            ' or "+ p ms2 777.00@cid30.00 [210.00-1200.00]
            ' or "+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]
            ' or "FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]"
            ' or "ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]"
            ' or "+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"

            ' This RegEx matches text like 1312.95@45.00 or 756.98@cid35.00 or 902.5721@etd120.55@cid20.00
            Const PARENTION_REGEX = "(?<ParentMZ>[0-9.]+)@(?<CollisionMode1>[a-z]*)(?<CollisionEnergy1>[0-9.]+)(@(?<CollisionMode2>[a-z]+)(?<CollisionEnergy2>[0-9.]+))?"

            ' This RegEx looks for "sa" prior to Full ms"
            Const SA_REGEX = " sa Full ms"

            Const MSX_REGEX = " Full msx "

            Dim intCharIndex As Integer
            Dim intStartIndex As Integer

            Dim strMZText As String

            Dim blnMatchFound As Boolean
            Dim blnSuccess As Boolean

            Dim blnSupplementalActivationEnabled As Boolean
            Dim blnMultiplexedMSnEnabled As Boolean

            Static reFindParentIon As Regex
            Static reFindSAFullMS As Regex
            Static reFindFullMSx As Regex

            Dim reMatchParentIon As Match

            Dim strCollisionEnergy As String
            Dim sngCollisionEngergy As Single

            Dim udtBestParentIon = New udtParentIonInfoType()
            udtBestParentIon.Clear()

            intMSLevel = 1
            dblParentIonMZ = 0
            strCollisionMode = String.Empty
            strMZText = String.Empty
            blnMatchFound = False

            If lstParentIons Is Nothing Then
                lstParentIons = New List(Of udtParentIonInfoType)
            Else
                lstParentIons.Clear()
            End If

            Try

                If reFindSAFullMS Is Nothing Then
                    reFindSAFullMS = New Regex(SA_REGEX, RegexOptions.IgnoreCase Or RegexOptions.Compiled)
                End If
                blnSupplementalActivationEnabled = reFindSAFullMS.IsMatch(strFilterText)

                If reFindFullMSx Is Nothing Then
                    reFindFullMSx = New Regex(MSX_REGEX, RegexOptions.IgnoreCase Or RegexOptions.Compiled)
                End If
                blnMultiplexedMSnEnabled = reFindFullMSx.IsMatch(strFilterText)

                blnSuccess = ExtractMSLevel(strFilterText, intMSLevel, strMZText)

                If blnSuccess Then
                    ' Use a RegEx to extract out the last parent ion mass listed
                    ' For example, grab 1312.95 out of "1312.95@45.00 [ 350.00-2000.00]"
                    ' or, grab 873.85 out of "1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
                    ' or, grab 756.98 out of "756.98@etd100.00 [50.00-2000.00]"
                    ' or, grab 748.371 out of "748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"
                    '
                    ' However, if using multiplex ms/ms (msx) then we return the first parent ion listed

                    ' For safety, remove any text after a square bracket
                    intCharIndex = strMZText.IndexOf("["c)
                    If intCharIndex > 0 Then
                        strMZText = strMZText.Substring(0, intCharIndex)
                    End If

                    If reFindParentIon Is Nothing Then
                        reFindParentIon = New Regex(PARENTION_REGEX, RegexOptions.IgnoreCase Or RegexOptions.Compiled)
                    End If

                    ' Find all of the parent ion m/z's present in strMZText
                    intStartIndex = 0
                    Do
                        reMatchParentIon = reFindParentIon.Match(strMZText, intStartIndex)

                        If Not reMatchParentIon.Success Then
                            ' Match not found
                            ' If strMZText only contains a number, we will parse it out later in this function
                            Exit Do
                        End If

                        ' Match found

                        dblParentIonMZ = Double.Parse(reMatchParentIon.Groups("ParentMZ").Value)
                        strCollisionMode = String.Empty
                        sngCollisionEngergy = 0

                        blnMatchFound = True

                        intStartIndex = reMatchParentIon.Index + reMatchParentIon.Length

                        strCollisionMode = GetCapturedValue(reMatchParentIon, "CollisionMode1")

                        strCollisionEnergy = GetCapturedValue(reMatchParentIon, "CollisionEnergy1")
                        If Not String.IsNullOrEmpty(strCollisionEnergy) Then
                            Single.TryParse(strCollisionEnergy, sngCollisionEngergy)
                        End If

                        Dim sngCollisionEngergy2 As Single
                        Dim strCollisionMode2 = GetCapturedValue(reMatchParentIon, "CollisionMode2")

                        If Not String.IsNullOrEmpty(strCollisionMode2) Then
                            Dim strCollisionEnergy2 = GetCapturedValue(reMatchParentIon, "CollisionEnergy2")
                            Single.TryParse(strCollisionEnergy2, sngCollisionEngergy2)
                        End If

                        Dim allowSecondaryActivation = True
                        If String.Equals(strCollisionMode, "ETD", StringComparison.InvariantCultureIgnoreCase) And Not String.IsNullOrEmpty(strCollisionMode2) Then
                            If String.Equals(strCollisionMode2, "CID", StringComparison.InvariantCultureIgnoreCase) Then
                                strCollisionMode = "ETciD"
                                allowSecondaryActivation = False
                            ElseIf String.Equals(strCollisionMode2, "HCD", StringComparison.InvariantCultureIgnoreCase) Then
                                strCollisionMode = "EThcD"
                                allowSecondaryActivation = False
                            End If
                        End If

                        If allowSecondaryActivation AndAlso Not String.IsNullOrEmpty(strCollisionMode) Then
                            If blnSupplementalActivationEnabled Then
                                strCollisionMode = "sa_" & strCollisionMode
                            End If
                        End If

                        Dim udtParentIonInfo As udtParentIonInfoType
                        With udtParentIonInfo
                            .MSLevel = intMSLevel
                            .ParentIonMZ = dblParentIonMZ
                            .CollisionMode = String.Copy(strCollisionMode)
                            .CollisionMode2 = String.Copy(strCollisionMode2)
                            .CollisionEnergy = sngCollisionEngergy
                            .CollisionEnergy2 = sngCollisionEngergy2
                        End With

                        lstParentIons.Add(udtParentIonInfo)

                        If Not blnMultiplexedMSnEnabled OrElse (lstParentIons.Count = 1 AndAlso blnMultiplexedMSnEnabled) Then
                            udtBestParentIon = udtParentIonInfo
                        End If

                    Loop While intStartIndex < strMZText.Length - 1

                    If blnMatchFound Then
                        ' Update the output values using udtBestParentIon
                        With udtBestParentIon
                            intMSLevel = .MSLevel
                            dblParentIonMZ = .ParentIonMZ
                            strCollisionMode = .CollisionMode
                        End With
                    Else
                        ' Match not found using RegEx
                        ' Use manual text parsing instead

                        intCharIndex = strMZText.LastIndexOf("@"c)
                        If intCharIndex > 0 Then
                            strMZText = strMZText.Substring(0, intCharIndex)
                            intCharIndex = strMZText.LastIndexOf(" "c)
                            If intCharIndex > 0 Then
                                strMZText = strMZText.Substring(intCharIndex + 1)
                            End If

                            Try
                                dblParentIonMZ = Double.Parse(strMZText)
                                blnMatchFound = True
                            Catch ex As Exception
                                dblParentIonMZ = 0
                            End Try

                        ElseIf strMZText.Length > 0 Then
                            ' Find the longest contiguous number that strMZText starts with

                            intCharIndex = -1
                            Do While intCharIndex < strMZText.Length - 1
                                If Char.IsNumber(strMZText.Chars(intCharIndex + 1)) OrElse strMZText.Chars(intCharIndex + 1) = "."c Then
                                    intCharIndex += 1
                                Else
                                    Exit Do
                                End If
                            Loop

                            If intCharIndex >= 0 Then
                                Try
                                    dblParentIonMZ = Double.Parse(strMZText.Substring(0, intCharIndex + 1))
                                    blnMatchFound = True
                                Catch ex As Exception
                                    dblParentIonMZ = 0
                                End Try
                            End If
                        End If
                    End If
                End If

            Catch ex As Exception
                blnMatchFound = False
            End Try

            ' ReSharper disable once NotAssignedOutParameter (ReSharper thinks lstParentIons is not being properly initialized even though it is)
            Return blnMatchFound

        End Function

        Public Shared Function ExtractMSLevel(ByVal strFilterText As String, <Out()> ByRef intMSLevel As Integer, <Out()> ByRef strMZText As String) As Boolean
            ' Looks for "Full ms2" or "Full ms3" or " p ms2" or "SRM ms2" in strFilterText
            ' Returns True if found and False if no match

            ' Populates intMSLevel with the number after "ms" and strMZText with the text after "ms2"

            Static reFindMS As Regex

            Dim intCharIndex As Integer
            Dim intMatchTextLength As Integer

            intMSLevel = 1
            intCharIndex = 0

            If reFindMS Is Nothing Then
                reFindMS = New Regex(MS2_REGEX, RegexOptions.IgnoreCase Or RegexOptions.Compiled)
            End If

            Dim reMatchMS = reFindMS.Match(strFilterText)

            If reMatchMS.Success Then
                intMSLevel = CInt(reMatchMS.Groups(2).Value)
                intCharIndex = strFilterText.IndexOf(reMatchMS.ToString(), StringComparison.InvariantCultureIgnoreCase)
                intMatchTextLength = reMatchMS.Length
            End If

            If intCharIndex > 0 Then
                ' Copy the text after "Full ms2" or "Full ms3" in strFilterText to strMZText
                strMZText = strFilterText.Substring(intCharIndex + intMatchTextLength).Trim
                Return True
            Else
                strMZText = String.Empty
                Return False
            End If

        End Function

        Protected Overrides Function FillFileInfo() As Boolean
            ' Populates the mFileInfo structure
            ' Function returns True if no error, False if an error

            Dim intResult As Integer

            Dim intIndex As Integer

            Dim intMethodCount As Integer

            Dim strMethod As String
            Try
                If mXRawFile Is Nothing Then Return False

                ' Make sure the MS controller is selected
                If Not SetMSController() Then Return False

                With mFileInfo

                    .CreationDate = Nothing
                    mXRawFile.GetCreationDate(.CreationDate)
                    mXRawFile.IsError(intResult)                        ' Unfortunately, .IsError() always returns 0, even if an error occurred
                    If intResult <> 0 Then Return False

                    .CreatorID = Nothing
                    mXRawFile.GetCreatorID(.CreatorID)

                    .InstFlags = Nothing
                    mXRawFile.GetInstFlags(.InstFlags)

                    .InstHardwareVersion = Nothing
                    mXRawFile.GetInstHardwareVersion(.InstHardwareVersion)

                    .InstSoftwareVersion = Nothing
                    mXRawFile.GetInstSoftwareVersion(.InstSoftwareVersion)

                    If Not mLoadMSMethodInfo Then
                        ReDim .InstMethods(-1)
                    Else

                        mXRawFile.GetNumInstMethods(intMethodCount)
                        ReDim .InstMethods(intMethodCount - 1)

                        For intIndex = 0 To intMethodCount - 1
                            strMethod = Nothing
                            mXRawFile.GetInstMethod(intIndex, strMethod)
                            If String.IsNullOrWhiteSpace(strMethod) Then
                                .InstMethods(intIndex) = String.Empty
                            Else
                                .InstMethods(intIndex) = String.Copy(strMethod)
                            End If

                        Next intIndex
                    End If

                    .InstModel = Nothing
                    .InstName = Nothing
                    .InstrumentDescription = Nothing
                    .InstSerialNumber = Nothing

                    mXRawFile.GetInstModel(.InstModel)
                    mXRawFile.GetInstName(.InstName)
                    mXRawFile.GetInstrumentDescription(.InstrumentDescription)
                    mXRawFile.GetInstSerialNumber(.InstSerialNumber)

                    mXRawFile.GetVersionNumber(.VersionNumber)
                    mXRawFile.GetMassResolution(.MassResolution)

                    mXRawFile.GetFirstSpectrumNumber(.ScanStart)
                    mXRawFile.GetLastSpectrumNumber(.ScanEnd)

                    .AcquisitionDate = Nothing
                    .AcquisitionFilename = Nothing
                    .Comment1 = Nothing
                    .Comment2 = Nothing
                    .SampleName = Nothing
                    .SampleComment = Nothing


                    ' Note that the following are typically blank
                    mXRawFile.GetAcquisitionDate(.AcquisitionDate)
                    mXRawFile.GetAcquisitionFileName(.AcquisitionFilename)
                    mXRawFile.GetComment1(.Comment1)
                    mXRawFile.GetComment2(.Comment2)
                    mXRawFile.GetSeqRowSampleName(.SampleName)
                    mXRawFile.GetSeqRowComment(.SampleComment)
                End With

                If Not mLoadMSTuneInfo Then
                    ReDim mFileInfo.TuneMethods(-1)
                Else
                    GetTuneData()
                End If

            Catch ex As Exception
                Dim strError As String = "Error: Exception in FillFileInfo: " & ex.Message
                RaiseErrorMessage(strError)
                Return False
            End Try

            Return True

        End Function

        Private Function GetActivationType(scan As Integer, msLevel As Integer) As ActivationTypeConstants

            Try

                Dim activationTypeCode = 0

                mXRawFile.GetActivationTypeForScanNum(scan, msLevel, activationTypeCode)

                Dim activationType As ActivationTypeConstants

                If Not [Enum].TryParse(Of ActivationTypeConstants)(activationTypeCode.ToString(), activationType) Then
                    activationType = ActivationTypeConstants.Unknown
                End If

                Return activationType

            Catch ex As Exception
                Dim strError As String = "Error: Exception in GetActivationType: " & ex.Message
                RaiseWarningMessage(strError)
                Return ActivationTypeConstants.Unknown
            End Try

        End Function

        Private Shared Function GetCapturedValue(reMatch As Match, captureGroupName As String) As String
            Dim capturedValue = reMatch.Groups(captureGroupName)

            If Not capturedValue Is Nothing Then
                If Not String.IsNullOrEmpty(capturedValue.Value) Then
                    Return capturedValue.Value
                End If
            End If

            Return String.Empty

        End Function

        Public Function GetCollisionEnergy(ByVal scan As Integer) As List(Of Double)

            Dim intNumMSOrders As Integer
            Dim lstCollisionEnergies = New List(Of Double)
            Dim dblCollisionEnergy As Double

            Try
                If mXRawFile Is Nothing Then Return lstCollisionEnergies

                mXRawFile.GetNumberOfMSOrdersFromScanNum(scan, intNumMSOrders)

                For intMSOrder = 1 To intNumMSOrders
                    dblCollisionEnergy = 0
                    mXRawFile.GetCollisionEnergyForScanNum(scan, intMSOrder, dblCollisionEnergy)

                    If (dblCollisionEnergy > 0) Then
                        lstCollisionEnergies.Add(dblCollisionEnergy)
                    End If
                Next

            Catch ex As Exception
                Dim strError As String = "Error: Exception in GetCollisionEnergy: " & ex.Message
                RaiseErrorMessage(strError)
            End Try

            Return lstCollisionEnergies

        End Function

        Public Overrides Function GetNumScans() As Integer
            ' Returns the number of scans, or -1 if an error

            Dim intResult As Integer
            Dim intScanCount As Integer

            Try
                If mXRawFile Is Nothing Then Return -1

                mXRawFile.GetNumSpectra(intScanCount)
                mXRawFile.IsError(intResult)            ' Unfortunately, .IsError() always returns 0, even if an error occurred
                If intResult = 0 Then
                    Return intScanCount
                Else
                    Return -1
                End If
            Catch ex As Exception
                Return -1
            End Try

        End Function

        ''' <summary>
        ''' Get the header info for the specified scan
        ''' </summary>
        ''' <param name="scan">Scan number</param>
        ''' <param name="udtScanHeaderInfo">Scan header info struct</param>
        ''' <returns>True if no error, False if an error</returns>
        ''' <remarks></remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Overrides Function GetScanInfo(ByVal scan As Integer, <Out()> ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Boolean

            Dim scanInfo As clsScanInfo = Nothing
            Dim success = GetScanInfo(scan, scanInfo)

            If success Then
                udtScanHeaderInfo = ScanInfoClassToStruct(scanInfo)
            Else
                udtScanHeaderInfo = New udtScanHeaderInfoType
            End If

            Return success
        End Function

        ''' <summary>
        ''' Get the header info for the specified scan
        ''' </summary>
        ''' <param name="scan">Scan number</param>
        ''' <param name="scanInfo">Scan header info class</param>
        ''' <returns>True if no error, False if an error</returns>
        ''' <remarks></remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Overrides Function GetScanInfo(ByVal scan As Integer, <Out()> ByRef scanInfo As clsScanInfo) As Boolean

            ' Check for the scan in the cache
            If mCachedScanInfo.TryGetValue(scan, scanInfo) Then
                Return True
            End If

            If scan < mFileInfo.ScanStart Then
                scan = mFileInfo.ScanStart
            ElseIf scan > mFileInfo.ScanEnd Then
                scan = mFileInfo.ScanEnd
            End If

            scanInfo = New clsScanInfo(scan)

            Try

                If mXRawFile Is Nothing Then Return False

                ' Make sure the MS controller is selected
                If Not SetMSController() Then Return False

                ' Initialize the values that will be populated using GetScanHeaderInfoForScanNum()
                scanInfo.NumPeaks = 0
                scanInfo.TotalIonCurrent = 0
                scanInfo.SIMScan = False
                scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM
                scanInfo.ZoomScan = False
                scanInfo.CollisionMode = String.Empty
                scanInfo.FilterText = String.Empty
                scanInfo.IonMode = IonModeConstants.Unknown

                Dim intBooleanVal = 0
                Dim intResult = 0

                mXRawFile.GetScanHeaderInfoForScanNum(
                  scan,
                  scanInfo.NumPeaks,
                  scanInfo.RetentionTime,
                  scanInfo.LowMass,
                  scanInfo.HighMass,
                  scanInfo.TotalIonCurrent,
                  scanInfo.BasePeakMZ,
                  scanInfo.BasePeakIntensity,
                  scanInfo.NumChannels,
                  intBooleanVal,
                  scanInfo.Frequency)

                mXRawFile.IsError(intResult)        ' Unfortunately, .IsError() always returns 0, even if an error occurred

                If intResult <> 0 Then Exit Try

                scanInfo.UniformTime = CBool(intBooleanVal)

                intBooleanVal = 0
                mXRawFile.IsCentroidScanForScanNum(scan, intBooleanVal)

                scanInfo.IsCentroided = CBool(intBooleanVal)

                Dim intArrayCount = 0
                Dim objLabels As Object = Nothing
                Dim objValues As Object = Nothing

                Try
                    If Not mCorruptMemoryEncountered Then
                        ' Retrieve the additional parameters for this scan (including Scan Event)
                        mXRawFile.GetTrailerExtraForScanNum(scan, objLabels, objValues, intArrayCount)
                    End If
                Catch ex As AccessViolationException
                    Dim strWarningMessage = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " & scan & ": " & ex.Message
                    RaiseWarningMessage(strWarningMessage)
                    intArrayCount = 0

                Catch ex As Exception
                    Dim strWarningMessage = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " & scan & ": " & ex.Message
                    RaiseWarningMessage(strWarningMessage)
                    intArrayCount = 0

                    If ex.Message.ToLower().Contains("memory is corrupt") Then
                        mCorruptMemoryEncountered = True
                    End If
                End Try

                scanInfo.EventNumber = 1
                If intArrayCount > 0 Then
                    Dim scanEventNames() As String
                    Dim scanEventValues() As String

                    scanEventNames = CType(objLabels, String())
                    scanEventValues = CType(objValues, String())

                    scanInfo.StoreScanEvents(scanEventNames, scanEventValues)

                    ' Look for the entry in strLabels named "Scan Event:"
                    ' Entries for the LCQ are:
                    '   Wideband Activation
                    '   Micro Scan Count
                    '   Ion Injection Time (ms)
                    '   Scan Segment
                    '   Scan Event
                    '   Elapsed Scan Time (sec)
                    '   API Source CID Energy
                    '   Resolution
                    '   Average Scan by Inst
                    '   BackGd Subtracted by Inst
                    '   Charge State

                    For Each scanEvent In From item In scanInfo.ScanEvents Where item.Key.ToLower().StartsWith("scan event")
                        Try
                            scanInfo.EventNumber = CInt(scanEvent.Value)
                        Catch ex As Exception
                            ' Ignore errors here
                        End Try
                        Exit For
                    Next

                End If

                ' Lookup the filter text for this scan
                ' Parse out the parent ion m/z for fragmentation scans
                ' Must set strFilterText to Nothing prior to calling .GetFilterForScanNum()
                Dim strFilterText As String = Nothing
                mXRawFile.GetFilterForScanNum(scan, strFilterText)

                scanInfo.FilterText = String.Copy(strFilterText)

                scanInfo.IsFTMS = ScanIsFTMS(strFilterText)

                If String.IsNullOrWhiteSpace(scanInfo.FilterText) Then scanInfo.FilterText = String.Empty

                If scanInfo.EventNumber <= 1 Then
                    Dim intMSLevel As Integer

                    ' XRaw periodically mislabels a scan as .EventNumber = 1 when it's really an MS/MS scan; check for this
                    If ExtractMSLevel(scanInfo.FilterText, intMSLevel, "") Then
                        scanInfo.EventNumber = intMSLevel
                    End If
                End If

                If scanInfo.EventNumber > 1 Then
                    ' MS/MS data
                    scanInfo.MSLevel = 2

                    If String.IsNullOrWhiteSpace(scanInfo.FilterText) Then
                        ' FilterText is empty; this indicates a problem with the .Raw file
                        ' This is rare, but does happen (see scans 2 and 3 in QC_Shew_08_03_pt5_1_MAXPRO_27Oct08_Raptor_08-01-01.raw)
                        ' We'll set the Parent Ion to 0 m/z and the collision mode to CID
                        scanInfo.ParentIonMZ = 0
                        scanInfo.CollisionMode = "cid"
                        If scanInfo.ActivationType = ActivationTypeConstants.Unknown Then
                            scanInfo.ActivationType = ActivationTypeConstants.CID
                        End If
                        scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM
                    Else
                        Dim dblParentIonMZ As Double
                        Dim intMSLevel As Integer
                        Dim strCollisionMode As String = String.Empty

                        ' Parse out the parent ion and collision energy from .FilterText
                        If ExtractParentIonMZFromFilterText(scanInfo.FilterText, dblParentIonMZ, intMSLevel, strCollisionMode) Then
                            scanInfo.ParentIonMZ = dblParentIonMZ
                            scanInfo.CollisionMode = strCollisionMode

                            If intMSLevel > 2 Then
                                scanInfo.MSLevel = intMSLevel
                            End If

                            ' Check whether this is an SRM MS2 scan
                            scanInfo.MRMScanType = DetermineMRMScanType(scanInfo.FilterText)
                        Else
                            ' Could not find "Full ms2" in .FilterText
                            ' XRaw periodically mislabels a scan as .EventNumber > 1 when it's really an MS scan; check for this
                            If ValidateMSScan(scanInfo.FilterText, scanInfo.MSLevel, scanInfo.SIMScan, scanInfo.MRMScanType, scanInfo.ZoomScan) Then
                                ' Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                            Else
                                ' Unknown format for .FilterText; return an error
                                RaiseErrorMessage("Unknown format for Scan Filter: " & scanInfo.FilterText)
                                Return False
                            End If
                        End If
                    End If
                Else
                    ' MS data
                    ' Make sure .FilterText contains one of the following:
                    '   FULL_MS_TEXT = "Full ms "
                    '   FULL_LOCK_MS_TEXT = "Full lock ms "
                    '   FULL_PR_TEXT = "Full pr "
                    '   SIM_MS_TEXT = "SIM ms "
                    '   SIM_PR_TEXT = "SIM pr "
                    '   MRM_Q1MS_TEXT = "Q1MS "
                    '   MRM_SRM_TEXT = "SRM "

                    If scanInfo.FilterText = String.Empty Then
                        ' FilterText is empty; this indicates a problem with the .Raw file
                        ' This is rare, but does happen (see scans 2 and 3 in QC_Shew_08_03_pt5_1_MAXPRO_27Oct08_Raptor_08-01-01.raw)
                        scanInfo.MSLevel = 1
                        scanInfo.SIMScan = False
                        scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM
                    Else

                        If ValidateMSScan(scanInfo.FilterText, scanInfo.MSLevel, scanInfo.SIMScan, scanInfo.MRMScanType, scanInfo.ZoomScan) Then
                            ' Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                        Else
                            ' Unknown format for .FilterText; return an error
                            RaiseErrorMessage("Unknown format for Scan Filter: " & scanInfo.FilterText)
                            Return False
                        End If
                    End If

                End If

                scanInfo.IonMode = DetermineIonizationMode(scanInfo.FilterText)

                ' Now that we know MSLevel we can lookup the activation type (aka activation method)
                scanInfo.ActivationType = GetActivationType(scan, scanInfo.MSLevel)

                If Not scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                    ' Parse out the MRM_QMS or SRM information for this scan
                    InitializeMRMInfo(scanInfo.MRMInfo, 1)
                    ExtractMRMMasses(scanInfo.FilterText, scanInfo.MRMScanType, scanInfo.MRMInfo)
                Else
                    InitializeMRMInfo(scanInfo.MRMInfo, 0)
                End If

                ' Retrieve the Status Log for this scan using the following
                ' The Status Log includes numerous instrument parameters, including voltages, temperatures, pressures, turbo pump speeds, etc. 
                intArrayCount = 0
                objLabels = Nothing
                objValues = Nothing

                Try
                    If Not mCorruptMemoryEncountered Then
                        Dim dblStatusLogRT As Double

                        mXRawFile.GetStatusLogForScanNum(scan, dblStatusLogRT, objLabels, objValues, intArrayCount)
                    End If
                Catch ex As AccessViolationException
                    Dim strWarningMessage = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " & scan & ": " & ex.Message
                    RaiseWarningMessage(strWarningMessage)
                    intArrayCount = 0

                Catch ex As Exception
                    Dim strWarningMessage = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " & scan & ": " & ex.Message
                    RaiseWarningMessage(strWarningMessage)
                    intArrayCount = 0

                    If ex.Message.ToLower().Contains("memory is corrupt") Then
                        mCorruptMemoryEncountered = True
                    End If
                End Try

                If intArrayCount > 0 Then
                    Dim logNames() As String
                    Dim logValues() As String

                    logNames = CType(objLabels, String())
                    logValues = CType(objValues, String())

                    scanInfo.StoreStatusLog(logNames, logValues)

                End If


            Catch ex As Exception
                Dim strError As String = "Error: Exception in GetScanInfo: " & ex.Message
                RaiseWarningMessage(strError)
                CacheScanInfo(scan, scanInfo)
                Return False
            End Try

            CacheScanInfo(scan, scanInfo)

            Return True

        End Function

        Public Shared Function GetScanTypeNameFromFinniganScanFilterText(ByVal strFilterText As String) As String

            ' Examines strFilterText to determine what the scan type is
            ' Examples:
            ' Given                                                                ScanTypeName
            ' ITMS + c ESI Full ms [300.00-2000.00]                                MS
            ' FTMS + p NSI Full ms [400.00-2000.00]                                HMS
            ' ITMS + p ESI d Z ms [579.00-589.00]                                  Zoom-MS
            ' ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]             CID-MSn
            ' ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]              PQD-MSn
            ' FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]             HCD-HMSn
            ' ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]          SA_ETD-MSn

            ' FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]         HCD-HMSn using multiplexed MSn (introduced with the Q-Exactive)

            ' + c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                       MSn
            ' + c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                          MSn
            ' ITMS + c NSI d Full ms10 421.76@35.00                                                MSn
            ' ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [242.00-248.00, 285.00-291.00]  CID-MSn

            ' + p ms2 777.00@cid30.00 [210.00-1200.00]                                             CID-MSn
            ' + c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                   CID-SRM
            ' + c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]    CID-SRM
            ' + p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                     Q1MS
            ' + p NSI Q3MS [150.070-1500.000]                                                      Q3MS
            ' c NSI Full cnl 162.053 [300.000-1200.000]                                            MRM_Full_NL
            
            ' Lumos scan filter examples
            ' FTMS + p NSI Full ms                                                                 HMS
            ' ITMS + c NSI r d Full ms2 916.3716@cid30.00 [247.0000-2000.0000]                     CID-MSn
            ' ITMS + c NSI r d Full ms2 916.3716@hcd30.00 [100.0000-2000.0000]                     HCD-MSn

            ' ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]       ETciD-MSn  (ETD fragmentation, then further fragmented by CID in the ion trap; detected with the ion trap)
            ' ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]       EThcD-MSn  (ETD fragmentation, then further fragmented by HCD in the ion routing multipole; detected with the ion trap)

            ' FTMS + c NSI r d Full ms2 744.0129@cid30.00 [199.0000-2000.0000]                     CID-HMSn
            ' FTMS + p NSI r d Full ms2 944.4316@hcd30.00 [100.0000-2000.0000]                     HCD-HMSn

            ' FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]       ETciD-HMSn  (ETD fragmentation, then further fragmented by CID in the ion trap; detected with orbitrap)
            ' FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]       EThcD-HMSn  (ETD fragmentation, then further fragmented by HCD in the ion routing multipole; detected with orbitrap)

            Dim strScanTypeName = "MS"
            Dim intMSLevel As Integer
            Dim dblParentIonMZ As Double
            Dim strCollisionMode As String
            Dim eMRMScanType As MRMScanTypeConstants

            Dim blnSIMScan As Boolean
            Dim blnZoomScan As Boolean

            Dim blnValidScanFilter As Boolean

            Try
                blnValidScanFilter = True
                intMSLevel = 1
                strCollisionMode = ""
                eMRMScanType = MRMScanTypeConstants.NotMRM
                blnSIMScan = False
                blnZoomScan = False

                If strFilterText.Length = 0 Then
                    strScanTypeName = "MS"
                    Exit Try
                End If

                If Not ExtractMSLevel(strFilterText, intMSLevel, "") Then
                    ' Assume this is an MS scan
                    intMSLevel = 1
                End If

                If intMSLevel > 1 Then
                    ' Parse out the parent ion and collision energy from strFilterText
                    If ExtractParentIonMZFromFilterText(strFilterText, dblParentIonMZ, intMSLevel, strCollisionMode) Then

                        ' Check whether this is an SRM MS2 scan
                        eMRMScanType = DetermineMRMScanType(strFilterText)
                    Else
                        ' Could not find "Full ms2" in strFilterText
                        ' XRaw periodically mislabels a scan as .EventNumber > 1 when it's really an MS scan; check for this
                        If ValidateMSScan(strFilterText, intMSLevel, blnSIMScan, eMRMScanType, blnZoomScan) Then
                            ' Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                        Else
                            ' Unknown format for strFilterText; return an error
                            blnValidScanFilter = False
                        End If
                    End If

                Else
                    ' Make sure strFilterText contains one of the following:
                    '   FULL_MS_TEXT = "Full ms "
                    '   FULL_LOCK_MS_TEXT = "Full lock ms "
                    '   FULL_PR_TEXT = "Full pr "
                    '   SIM_MS_TEXT = "SIM ms "
                    '   SIM_PR_TEXT = "SIM pr "
                    '   MRM_Q1MS_TEXT = "Q1MS "
                    '   MRM_SRM_TEXT = "SRM "
                    If ValidateMSScan(strFilterText, intMSLevel, blnSIMScan, eMRMScanType, blnZoomScan) Then
                        ' Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                    Else
                        ' Unknown format for strFilterText; return an error
                        blnValidScanFilter = False
                    End If
                End If


                If blnValidScanFilter Then
                    If eMRMScanType = MRMScanTypeConstants.NotMRM Then
                        If blnSIMScan Then
                            strScanTypeName = SIM_MS_TEXT.Trim
                        ElseIf blnZoomScan Then
                            strScanTypeName = "Zoom-MS"
                        Else

                            ' Normal, plain MS or MSn scan

                            If intMSLevel > 1 Then
                                strScanTypeName = "MSn"
                            Else
                                strScanTypeName = "MS"
                            End If

                            If ScanIsFTMS(strFilterText) Then
                                ' HMS or HMSn scan
                                strScanTypeName = "H" & strScanTypeName
                            End If

                            If intMSLevel > 1 AndAlso strCollisionMode.Length > 0 Then
                                strScanTypeName = CapitalizeCollisionMode(strCollisionMode) & "-" & strScanTypeName
                            End If

                        End If
                    Else
                        ' This is an MRM or SRM scan

                        Select Case eMRMScanType
                            Case MRMScanTypeConstants.MRMQMS
                                If ContainsText(strFilterText, MRM_Q1MS_TEXT, 1) Then
                                    strScanTypeName = MRM_Q1MS_TEXT.Trim

                                ElseIf ContainsText(strFilterText, MRM_Q3MS_TEXT, 1) Then
                                    strScanTypeName = MRM_Q3MS_TEXT.Trim
                                Else
                                    ' Unknown QMS mode
                                    strScanTypeName = "MRM QMS"
                                End If

                            Case MRMScanTypeConstants.SRM
                                If strCollisionMode.Length > 0 Then
                                    strScanTypeName = strCollisionMode.ToUpper() & "-SRM"
                                Else
                                    strScanTypeName = "CID-SRM"
                                End If


                            Case MRMScanTypeConstants.FullNL
                                strScanTypeName = "MRM_Full_NL"

                            Case Else
                                strScanTypeName = "MRM"
                        End Select

                    End If


                End If

            Catch ex As Exception

            End Try

            Return strScanTypeName

        End Function

        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Protected Sub GetTuneData()

            ' Note that intTuneMethodCount is set to 0, but we initially reserve space for intNumTuneData methods
            Dim intTuneMethodCountValid = 0
            Dim strWarningMessage As String

            Dim intNumTuneData As Integer
            mXRawFile.GetNumTuneData(intNumTuneData)
            ReDim mFileInfo.TuneMethods(intNumTuneData - 1)

            For intIndex = 0 To intNumTuneData - 1
                Dim intTuneLabelCount = 0
                Dim objLabels As Object = Nothing
                Dim objValues As Object = Nothing

                Try
                    If Not mCorruptMemoryEncountered Then
                        mXRawFile.GetTuneData(intIndex, objLabels, objValues, intTuneLabelCount)
                    End If

                Catch ex As AccessViolationException
                    strWarningMessage = "Unable to load tune data; possibly a corrupt .Raw file"
                    RaiseWarningMessage(strWarningMessage)
                    Exit For

                Catch ex As Exception
                    ' Exception getting TuneData
                    strWarningMessage = "Warning: Exception calling mXRawFile.GetTuneData for Index " & intIndex.ToString & ": " & ex.Message
                    RaiseWarningMessage(strWarningMessage)
                    intTuneLabelCount = 0

                    If ex.Message.ToLower().Contains("memory is corrupt") Then
                        mCorruptMemoryEncountered = True
                        Exit For
                    End If
                End Try


                If intTuneLabelCount > 0 Then
                    strWarningMessage = String.Empty
                    If objLabels Is Nothing Then
                        ' .GetTuneData returned a non-zero count, but no parameter names; unable to continue
                        strWarningMessage = "Warning: the GetTuneData function returned a positive tune parameter count but no parameter names"
                    ElseIf objValues Is Nothing Then
                        ' .GetTuneData returned parameter names, but objValues is nothing; unable to continue
                        strWarningMessage = "Warning: the GetTuneData function returned tune parameter names but no tune values"
                    End If

                    If strWarningMessage.Length > 0 Then
                        strWarningMessage &= " (Tune Method " & (intIndex + 1).ToString & ")"
                        RaiseWarningMessage(strWarningMessage)
                        intTuneLabelCount = 0
                    End If

                End If

                If intTuneLabelCount > 0 Then
                    If intTuneMethodCountValid >= mFileInfo.TuneMethods.Length Then
                        ReDim Preserve mFileInfo.TuneMethods(mFileInfo.TuneMethods.Length * 2 - 1)
                    End If

                    With mFileInfo.TuneMethods(intTuneMethodCountValid)

                        ' Note that .Count is initially 0, but we reserve space for intTuneLabelCount settings
                        .Count = 0
                        ReDim .SettingCategory(intTuneLabelCount - 1)
                        ReDim .SettingName(intTuneLabelCount - 1)
                        ReDim .SettingValue(intTuneLabelCount - 1)

                        If intTuneLabelCount > 0 Then

                            Dim strTuneSettingNames = CType(objLabels, String())
                            Dim strTuneSettingValues = CType(objValues, String())

                            ' Step through the names and store in the .Setting() arrays
                            Dim strTuneCategory = "General"
                            For intSettingIndex = 0 To intTuneLabelCount - 1
                                If strTuneSettingValues(intSettingIndex).Length = 0 AndAlso
                                   Not strTuneSettingNames(intSettingIndex).EndsWith(":") Then
                                    ' New category
                                    If strTuneSettingNames(intSettingIndex).Length > 0 Then
                                        strTuneCategory = String.Copy(strTuneSettingNames(intSettingIndex))
                                    Else
                                        strTuneCategory = "General"
                                    End If
                                Else
                                    .SettingCategory(.Count) = String.Copy(strTuneCategory)
                                    .SettingName(.Count) = strTuneSettingNames(intSettingIndex).TrimEnd(":"c)
                                    .SettingValue(.Count) = String.Copy(strTuneSettingValues(intSettingIndex))

                                    .Count += 1
                                End If

                            Next intSettingIndex

                            If .Count < .SettingName.Length Then
                                ReDim Preserve .SettingCategory(.Count - 1)
                                ReDim Preserve .SettingName(.Count - 1)
                                ReDim Preserve .SettingValue(.Count - 1)
                            End If
                        End If
                    End With
                    intTuneMethodCountValid += 1

                    If intTuneMethodCountValid > 1 Then
                        ' Compare this tune method to the previous one; if identical, then don't keep it
                        If TuneMethodsMatch(mFileInfo.TuneMethods(intTuneMethodCountValid - 2), mFileInfo.TuneMethods(intTuneMethodCountValid - 1)) Then
                            intTuneMethodCountValid -= 1
                        End If
                    End If
                End If

            Next intIndex

            If mFileInfo.TuneMethods.Length <> intTuneMethodCountValid Then
                ReDim Preserve mFileInfo.TuneMethods(intTuneMethodCountValid - 1)
            End If

        End Sub

        Public Shared Function MakeGenericFinniganScanFilter(ByVal strFilterText As String) As String

            ' Will make a generic version of the FilterText in strFilterText
            ' Examples:
            ' From                                                                 To
            ' ITMS + c ESI Full ms [300.00-2000.00]                                ITMS + c ESI Full ms
            ' FTMS + p NSI Full ms [400.00-2000.00]                                FTMS + p NSI Full ms
            ' ITMS + p ESI d Z ms [579.00-589.00]                                  ITMS + p ESI d Z ms
            ' ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]             ITMS + c ESI d Full ms2 0@cid35.00
            ' ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]              ITMS + c NSI d Full ms2 0@pqd27.00
            ' FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]             FTMS + c NSI d Full ms2 0@hcd40.00
            ' ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]          ITMS + c NSI d sa Full ms2 0@etd100.00

            ' + c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                       + c d Full ms2 0@45.00
            ' + c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]          + c d Full ms3 0@45.00 0@45.00
            ' ITMS + c NSI d Full ms10 421.76@35.00                                ITMS + c NSI d Full ms10 0@35.00

            ' + p ms2 777.00@cid30.00 [210.00-1200.00]                             + p ms2 0@cid30.00
            ' + c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]   + c NSI SRM ms2 0@cid15.00
            ' + c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]    + c NSI SRM ms2
            ' + p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]     + p NSI Q1MS
            ' + p NSI Q3MS [150.070-1500.000]                                      + p NSI Q3MS
            ' c NSI Full cnl 162.053 [300.000-1200.000]                            c NSI Full cnl

            Const COLLISION_SPEC_REGEX = "(?<MzValue> [0-9.]+)@"


            Dim strGenericScanFilterText = "MS"
            Dim intCharIndex As Integer

            Dim reCollisionSpecs As Regex

            Try
                If String.IsNullOrWhiteSpace(strFilterText) Then Exit Try

                strGenericScanFilterText = String.Copy(strFilterText)

                ' First look for and remove numbers between square brackets
                intCharIndex = strGenericScanFilterText.IndexOf("["c)
                If intCharIndex > 0 Then
                    strGenericScanFilterText = strGenericScanFilterText.Substring(0, intCharIndex).TrimEnd(" "c)
                Else
                    strGenericScanFilterText = strGenericScanFilterText.TrimEnd(" "c)
                End If

                intCharIndex = strGenericScanFilterText.IndexOf(MRM_FullNL_TEXT, StringComparison.InvariantCultureIgnoreCase)
                If intCharIndex > 0 Then
                    ' MRM neutral loss
                    ' Remove any text after MRM_FullNL_TEXT
                    strGenericScanFilterText = strGenericScanFilterText.Substring(0, intCharIndex + MRM_FullNL_TEXT.Length).Trim
                    Exit Try
                End If

                ' Replace any digits before any @ sign with a 0
                If strGenericScanFilterText.IndexOf("@"c) > 0 Then
                    reCollisionSpecs = New Regex(COLLISION_SPEC_REGEX, RegexOptions.Compiled)

                    strGenericScanFilterText = reCollisionSpecs.Replace(strGenericScanFilterText, " 0@")
                End If

            Catch ex As Exception
                ' Ignore errors
            End Try

            Return strGenericScanFilterText

        End Function

        Private Shared Function ScanIsFTMS(ByVal strFilterText As String) As Boolean

            Return ContainsText(strFilterText, "FTMS", 0)

        End Function

        Private Function ScanInfoClassToStruct(ByVal scanInfo As clsScanInfo) As udtScanHeaderInfoType

            Dim udtScanHeaderInfo = New udtScanHeaderInfoType()

            With udtScanHeaderInfo

                .MSLevel = scanInfo.MSLevel
                .EventNumber = scanInfo.EventNumber
                .SIMScan = scanInfo.SIMScan
                .MRMScanType = scanInfo.MRMScanType
                .ZoomScan = scanInfo.ZoomScan

                .NumPeaks = scanInfo.NumPeaks
                .RetentionTime = scanInfo.RetentionTime
                .LowMass = scanInfo.LowMass
                .HighMass = scanInfo.HighMass
                .TotalIonCurrent = scanInfo.TotalIonCurrent
                .BasePeakMZ = scanInfo.BasePeakMZ
                .BasePeakIntensity = scanInfo.BasePeakIntensity

                .FilterText = scanInfo.FilterText
                .ParentIonMZ = scanInfo.ParentIonMZ
                .ActivationType = scanInfo.ActivationType
                .CollisionMode = scanInfo.CollisionMode
                .IonMode = scanInfo.IonMode
                .MRMInfo = scanInfo.MRMInfo

                .NumChannels = scanInfo.NumChannels
                .UniformTime = scanInfo.UniformTime
                .Frequency = scanInfo.Frequency
                .IsCentroidScan = scanInfo.IsCentroided

                Dim scanEvents = scanInfo.ScanEvents
                Dim statusLogs = scanInfo.StatusLog

                ReDim .ScanEventNames(scanEvents.Count - 1)
                ReDim .ScanEventValues(scanEvents.Count - 1)

                For i = 0 To scanEvents.Count - 1
                    .ScanEventNames(i) = scanEvents(i).Key
                    .ScanEventValues(i) = scanEvents(i).Value
                Next

                ReDim .StatusLogNames(statusLogs.Count - 1)
                ReDim .StatusLogValues(statusLogs.Count - 1)

                For i = 0 To statusLogs.Count - 1
                    .StatusLogNames(i) = statusLogs(i).Key
                    .StatusLogValues(i) = statusLogs(i).Value
                Next

            End With

            Return udtScanHeaderInfo

        End Function

        Private Function SetMSController() As Boolean
            ' A controller is typically the MS, UV, analog, etc.
            ' See ControllerTypeConstants

            Dim intResult As Integer

            mXRawFile.SetCurrentController(ControllerTypeConstants.MS, 1)
            mXRawFile.IsError(intResult)        ' Unfortunately, .IsError() always returns 0, even if an error occurred

            If intResult = 0 Then
                Return True
            Else
                Return False
            End If

        End Function

        ''' <summary>
        ''' Examines strFilterText to validate that it is a supported MS1 scan type (MS, SIM, or MRMQMS, or SRM scan)
        ''' </summary>
        ''' <param name="strFilterText"></param>
        ''' <param name="intMSLevel"></param>
        ''' <param name="blnSIMScan"></param>
        ''' <param name="eMRMScanType"></param>
        ''' <param name="blnZoomScan"></param>
        ''' <returns>True if strFilterText contains a known MS scan type</returns>
        ''' <remarks>Returns false for MSn scans (like ms2 or ms3)</remarks>
        Public Shared Function ValidateMSScan(ByVal strFilterText As String,
           <Out()> ByRef intMSLevel As Integer,
           <Out()> ByRef blnSIMScan As Boolean,
           <Out()> ByRef eMRMScanType As MRMScanTypeConstants,
           <Out()> ByRef blnZoomScan As Boolean) As Boolean

            Dim blnValidScan As Boolean

            intMSLevel = 0
            blnSIMScan = False
            eMRMScanType = MRMScanTypeConstants.NotMRM
            blnZoomScan = False

            Dim ms1Tags = New List(Of String) From {FULL_MS_TEXT, MS_ONLY_C_TEXT, MS_ONLY_P_TEXT, FULL_PR_TEXT, FULL_LOCK_MS_TEXT}

            Dim zoomTags = New List(Of String) From {MS_ONLY_Z_TEXT, MS_ONLY_PZ_TEXT, MS_ONLY_DZ_TEXT}

            If ContainsAny(strFilterText, ms1Tags, 1) Then
                ' This is really a Full MS scan
                intMSLevel = 1
                blnSIMScan = False
                blnValidScan = True
            Else
                If ContainsText(strFilterText, SIM_MS_TEXT, 1) Then
                    ' This is really a SIM MS scan
                    intMSLevel = 1
                    blnSIMScan = True
                    blnValidScan = True
                ElseIf ContainsAny(strFilterText, zoomTags, 1) Then
                    intMSLevel = 1
                    blnZoomScan = True
                    blnValidScan = True
                ElseIf ContainsText(strFilterText, MS_ONLY_PZ_MS2_TEXT, 1) Then
                    ' Technically, this should have MSLevel = 2, but that would cause a bunch of problems elsewhere in MASIC
                    ' Thus, we'll pretend it's MS1
                    intMSLevel = 1
                    blnZoomScan = True
                    blnValidScan = True
                Else
                    eMRMScanType = DetermineMRMScanType(strFilterText)
                    Select Case eMRMScanType
                        Case MRMScanTypeConstants.MRMQMS
                            intMSLevel = 1
                            blnValidScan = True            ' ToDo: Add support for TSQ MRMQMS data
                        Case MRMScanTypeConstants.SRM
                            intMSLevel = 2
                            blnValidScan = True            ' ToDo: Add support for TSQ SRM data
                        Case MRMScanTypeConstants.FullNL
                            intMSLevel = 2
                            blnValidScan = True            ' ToDo: Add support for TSQ Full NL data
                        Case Else
                            blnValidScan = False
                    End Select
                End If
            End If

            Return blnValidScan
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMZList"></param>
        ''' <param name="dblIntensityList"></param>
        ''' <param name="udtScanHeaderInfo">Unused; parameter retained for compatibility reasons</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")>
        Public Overloads Function GetScanData(ByVal scan As Integer, <Out()> ByRef dblMZList() As Double, <Out()> ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Integer
            Const intMaxNumberOfPeaks = 0
            Const blnCentroid = False
            Return GetScanData(scan, dblMZList, dblIntensityList, intMaxNumberOfPeaks, blnCentroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMZList"></param>
        ''' <param name="dblIntensityList"></param>
        ''' <param name="udtScanHeaderInfo">Unused; parameter retained for compatibility reasons</param>
        ''' <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")>
        Public Overloads Function GetScanData(ByVal scan As Integer, <Out()> ByRef dblMZList() As Double, <Out()> ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType, ByVal blnCentroid As Boolean) As Integer
            Const intMaxNumberOfPeaks = 0
            Return GetScanData(scan, dblMZList, dblIntensityList, intMaxNumberOfPeaks, blnCentroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMZList"></param>
        ''' <param name="dblIntensityList"></param>
        ''' <param name="udtScanHeaderInfo">Unused; parameter retained for compatibility reasons</param>
        ''' <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")>
        Public Overloads Function GetScanData(ByVal scan As Integer, <Out()> ByRef dblMZList() As Double, <Out()> ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType, ByVal intMaxNumberOfPeaks As Integer) As Integer
            Const blnCentroid = False
            Return GetScanData(scan, dblMZList, dblIntensityList, intMaxNumberOfPeaks, blnCentroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMZList"></param>
        ''' <param name="dblIntensityList"></param>
        ''' <param name="udtScanHeaderInfo">Unused; parameter retained for compatibility reasons</param>
        ''' <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        ''' <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")>
        Public Overloads Function GetScanData(ByVal scan As Integer, <Out()> ByRef dblMZList() As Double, <Out()> ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType, ByVal intMaxNumberOfPeaks As Integer, ByVal blnCentroid As Boolean) As Integer
            Return GetScanData(scan, dblMZList, dblIntensityList, intMaxNumberOfPeaks, blnCentroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scanNumber">Scan number</param>
        ''' <param name="mzList">Output array of mass values</param>
        ''' <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        Public Overloads Overrides Function GetScanData(ByVal scanNumber As Integer, <Out()> ByRef mzList() As Double, <Out()> ByRef intensityList() As Double) As Integer
            Const intMaxNumberOfPeaks = 0
            Const blnCentroid = False
            Return GetScanData(scanNumber, mzList, intensityList, intMaxNumberOfPeaks, blnCentroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity list for the specified scan
        ''' </summary>
        ''' <param name="scanNumber">Scan number</param>
        ''' <param name="mzList">Output array of mass values</param>
        ''' <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        ''' <param name="maxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        Public Overloads Overrides Function GetScanData(ByVal scanNumber As Integer, <Out()> ByRef mzList() As Double, <Out()> ByRef intensityList() As Double, ByVal maxNumberOfPeaks As Integer) As Integer
            Const centroid = False
            Return GetScanData(scanNumber, mzList, intensityList, maxNumberOfPeaks, centroid)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity for the specified scan
        ''' </summary>
        ''' <param name="scan">Scan number</param>
        ''' <param name="dblMZList">Output array of mass values</param>
        ''' <param name="dblIntensityList">Output array of intensity values (parallel to mzList)</param>
        ''' <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        ''' <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        Public Overloads Function GetScanData(ByVal scan As Integer, <Out()> ByRef dblMZList() As Double, <Out()> ByRef dblIntensityList() As Double, ByVal intMaxNumberOfPeaks As Integer, ByVal blnCentroid As Boolean) As Integer

            Dim dblMassIntensityPairs(,) As Double = Nothing

            Dim dataCount As Integer = GetScanData2D(scan, dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid)

            Try
                If dataCount <= 0 Then
                    ReDim dblMZList(-1)
                    ReDim dblIntensityList(-1)
                    Return 0
                End If

                If dblMassIntensityPairs.GetUpperBound(1) + 1 < dataCount Then
                    dataCount = dblMassIntensityPairs.GetUpperBound(1) + 1
                End If

                ReDim dblMZList(dataCount - 1)
                ReDim dblIntensityList(dataCount - 1)
                Dim sortRequired = False

                For intIndex = 0 To dataCount - 1
                    dblMZList(intIndex) = dblMassIntensityPairs(0, intIndex)
                    dblIntensityList(intIndex) = dblMassIntensityPairs(1, intIndex)

                    ' Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z, 
                    ' we have observed a few cases in certain scans of certain datasets that points with 
                    ' similar m/z values are swapped and ths slightly out of order
                    ' The following if statement checks for this
                    If (intIndex > 0 AndAlso dblMZList(intIndex) < dblMZList(intIndex - 1)) Then
                        sortRequired = True
                    End If

                Next intIndex

                If sortRequired Then
                    Array.Sort(dblMZList, dblIntensityList)
                End If

            Catch
                ReDim dblMZList(-1)
                ReDim dblIntensityList(-1)
                dataCount = -1
            End Try

            Return dataCount

        End Function

        ''' <summary>
        ''' Obtain the mass and intensity for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        Public Function GetScanData2D(ByVal scan As Integer, <Out()> ByRef dblMassIntensityPairs(,) As Double) As Integer
            Return GetScanData2D(scan, dblMassIntensityPairs, intMaxNumberOfPeaks:=0, blnCentroid:=False)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        ''' <param name="udtScanHeaderInfo">Unused; parameter retained for compatibility reasons</param>
        ''' <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <Obsolete("This method is deprecated, use GetScanData2D that does not use udtScanHeaderInfo")>
        Public Function GetScanData2D(ByVal scan As Integer, <Out()> ByRef dblMassIntensityPairs(,) As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType, ByVal intMaxNumberOfPeaks As Integer) As Integer
            Return GetScanData2D(scan, dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid:=False)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        ''' <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        Public Function GetScanData2D(ByVal scan As Integer, <Out()> ByRef dblMassIntensityPairs(,) As Double, ByVal intMaxNumberOfPeaks As Integer) As Integer
            Return GetScanData2D(scan, dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid:=False)
        End Function

        ''' <summary>
        ''' Obtain the mass and intensity for the specified scan
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        ''' <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        ''' <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Function GetScanData2D(
            ByVal scan As Integer,
            <Out()> ByRef dblMassIntensityPairs(,) As Double,
            ByVal intMaxNumberOfPeaks As Integer,
            ByVal blnCentroid As Boolean) As Integer

            ' Note that we're using function attribute HandleProcessCorruptedStateExceptions
            ' to force .NET to properly catch critical errors thrown by the XRawfile DLL

            Dim dataCount As Integer

            Dim strFilter As String
            Dim intIntensityCutoffValue As Integer

            Dim MassIntensityPairsList As Object = Nothing
            Dim PeakList As Object = Nothing

            dataCount = 0

            If scan < mFileInfo.ScanStart Then
                scan = mFileInfo.ScanStart
            ElseIf scan > mFileInfo.ScanEnd Then
                scan = mFileInfo.ScanEnd
            End If

            Dim scanInfo As clsScanInfo = Nothing

            If Not GetScanInfo(scan, scanInfo) Then
                Throw New Exception("Cannot retrieve ScanInfo from cache for scan " & scan & "; cannot retrieve scan data")
            End If

            Try
                If mXRawFile Is Nothing Then
                    Exit Try
                End If

                ' Make sure the MS controller is selected
                If Not SetMSController() Then
                    Exit Try
                End If

                strFilter = String.Empty            ' Could use this to filter the data returned from the scan; must use one of the filters defined in the file (see .GetFilters())
                intIntensityCutoffValue = 0

                If intMaxNumberOfPeaks < 0 Then intMaxNumberOfPeaks = 0

                If blnCentroid AndAlso scanInfo.IsCentroided Then
                    ' The scan data is already centroided; don't try to re-centroid
                    blnCentroid = False
                End If

                If blnCentroid AndAlso scanInfo.IsFTMS Then
                    ' Centroiding is enabled, and the dataset was acquired on an Orbitrap, Exactive, or FTMS instrument 

                    Dim massIntensityLabels As Object = Nothing
                    Dim labelFlags As Object = Nothing

                    mXRawFile.GetLabelData(massIntensityLabels, labelFlags, scan)

                    Dim dblMassIntensityLabels As Double(,)

                    dblMassIntensityLabels = CType(massIntensityLabels, Double(,))
                    dataCount = dblMassIntensityLabels.GetLength(1)

                    If dataCount > 0 Then
                        ReDim dblMassIntensityPairs(1, dataCount - 1)

                        For i = 0 To dataCount - 1
                            dblMassIntensityPairs(0, i) = dblMassIntensityLabels(0, i)  ' m/z
                            dblMassIntensityPairs(1, i) = dblMassIntensityLabels(1, i)  ' Intensity
                        Next

                    Else
                        ReDim dblMassIntensityPairs(-1, -1)
                    End If

                    ' Dim byteFlags As Byte(,)
                    ' byteFlags = CType(labelFlags, Byte(,))

                Else
                    ' Warning: The masses reported by GetMassListFromScanNum when centroiding are not properly calibrated and thus could be off by 0.3 m/z or more
                    '          That is why we use mXRawFile.GetLabelData() when centroiding profile-mode FTMS data (see ~25 lines above this comment)
                    '
                    '          For example, in scan 8101 of dataset RAW_Franc_Salm_IMAC_0h_R1A_18Jul13_Frodo_13-04-15, we see these values:
                    '           Profile m/z         Centroid m/z	Delta_PPM
                    '			112.051 			112.077			232
                    '			652.3752			652.4645		137
                    '			1032.56495			1032.6863		118
                    '			1513.7252			1513.9168		127

                    Dim intCentroidResult As Integer
                    Dim dblCentroidPeakWidth As Double = 0

                    If blnCentroid Then
                        intCentroidResult = 1
                    Else
                        intCentroidResult = 0
                    End If

                    mXRawFile.GetMassListFromScanNum(scan, strFilter, IntensityCutoffTypeConstants.None,
                       intIntensityCutoffValue, intMaxNumberOfPeaks, intCentroidResult, dblCentroidPeakWidth,
                       MassIntensityPairsList, PeakList, dataCount)

                    If dataCount > 0 Then
                        dblMassIntensityPairs = CType(MassIntensityPairsList, Double(,))
                    Else
                        ReDim dblMassIntensityPairs(-1, -1)
                    End If

                End If

                Return dataCount

            Catch ex As AccessViolationException
                Dim strError As String = "Unable to load data for scan " & scan & "; possibly a corrupt .Raw file"
                RaiseWarningMessage(strError)

            Catch ex As Exception

                Dim strError As String = "Unable to load data for scan " & scan & ": " & ex.Message & "; possibly a corrupt .Raw file"
                RaiseErrorMessage(strError)

            End Try

            ReDim dblMassIntensityPairs(-1, -1)
            Return -1

        End Function

        ''' <summary>
        ''' Gets the scan label data for an FTMS-tagged scan
        ''' </summary>
        ''' <param name="scan">Scan number</param>
        ''' <param name="ftLabelData">List of mass, intensity, resolution, baseline intensity, noise floor, and charge for each data point</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks></remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Function GetScanLabelData(ByVal scan As Integer, <Out()> ByRef ftLabelData() As udtFTLabelInfoType) As Integer

            ' Note that we're using function attribute HandleProcessCorruptedStateExceptions
            ' to force .NET to properly catch critical errors thrown by the XRawfile DLL

            If scan < mFileInfo.ScanStart Then
                scan = mFileInfo.ScanStart
            ElseIf scan > mFileInfo.ScanEnd Then
                scan = mFileInfo.ScanEnd
            End If

            Dim scanInfo As clsScanInfo = Nothing

            If Not GetScanInfo(scan, scanInfo) Then
                Throw New Exception("Cannot retrieve ScanInfo from cache for scan " & scan & "; cannot retrieve scan data")
            End If

            Try
                If mXRawFile Is Nothing Then
                    Exit Try
                End If

                If Not scanInfo.IsFTMS Then
                    Dim strWarningMessage = "Scan " & scan & " is not an FTMS scan; function GetScanLabelData cannot be used with this scan"
                    RaiseWarningMessage(strWarningMessage)
                    Exit Try
                End If

                Dim labelData As Object = Nothing
                Dim labelFlags As Object = Nothing

                mXRawFile.GetLabelData(labelData, labelFlags, scan)

                Dim labelDataArray As Double(,)
                labelDataArray = CType(labelData, Double(,))

                Dim dataCount = labelDataArray.GetLength(1)
                Dim maxColIndex = labelDataArray.GetLength(0) - 1

                If dataCount > 0 Then
                    ReDim ftLabelData(dataCount - 1)

                    For i = 0 To dataCount - 1
                        Dim labelInfo As New udtFTLabelInfoType

                        labelInfo.Mass = labelDataArray(0, i)
                        labelInfo.Intensity = labelDataArray(1, i)

                        If maxColIndex >= 2 Then
                            labelInfo.Resolution = CType(labelDataArray(2, i), Single)
                        End If

                        If maxColIndex >= 3 Then
                            labelInfo.Baseline = CType(labelDataArray(3, i), Single)
                        End If

                        If maxColIndex >= 4 Then
                            labelInfo.Noise = CType(labelDataArray(4, i), Single)
                        End If

                        If maxColIndex >= 5 Then
                            labelInfo.Charge = CType(labelDataArray(5, i), Integer)
                        End If

                        ftLabelData(i) = labelInfo
                    Next

                Else
                    ReDim ftLabelData(-1)
                End If

                Return dataCount

            Catch ex As AccessViolationException
                Dim strError As String = "Unable to load data for scan " & scan & "; possibly a corrupt .Raw file"
                RaiseWarningMessage(strError)

            Catch ex As Exception

                Dim strError As String = "Unable to load data for scan " & scan & ": " & ex.Message & "; possibly a corrupt .Raw file"
                RaiseErrorMessage(strError)

            End Try

            ReDim ftLabelData(-1)
            Return -1

        End Function

        ''' <summary>
        ''' Gets scan precision data for FTMS data (resolution of each data point)
        ''' </summary>
        ''' <param name="scan"></param>
        ''' <param name="massResolutionData">List of Intensity, Mass, AccuracyMMU, AccuracyPPM, and Resolution for each data point</param>
        ''' <returns>The number of data points, or -1 if an error</returns>
        ''' <remarks>This returns a subset of the data thatGetScanLabelData does, but with 2 additional fields.</remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Function GetScanPrecisionData(ByVal scan As Integer, <Out()> ByRef massResolutionData() As udtMassPrecisionInfoType) As Integer

            ' Note that we're using function attribute HandleProcessCorruptedStateExceptions
            ' to force .NET to properly catch critical errors thrown by the XRawfile DLL

            Dim dataCount As Integer

            If scan < mFileInfo.ScanStart Then
                scan = mFileInfo.ScanStart
            ElseIf scan > mFileInfo.ScanEnd Then
                scan = mFileInfo.ScanEnd
            End If

            Dim scanInfo As clsScanInfo = Nothing

            If Not GetScanInfo(scan, scanInfo) Then
                Throw New Exception("Cannot retrieve ScanInfo from cache for scan " & scan & "; cannot retrieve scan data")
            End If

            Try
                If mXRawFile Is Nothing Then
                    Exit Try
                End If

                If Not scanInfo.IsFTMS Then
                    Dim strWarningMessage = "Scan " & scan & " is not an FTMS scan; function GetScanLabelData cannot be used with this scan"
                    RaiseWarningMessage(strWarningMessage)
                    Exit Try
                End If

                Dim massResolutionDataList As Object = Nothing

                mXRawFile.GetMassPrecisionEstimate(scan, massResolutionDataList, dataCount)

                Dim massPrecisionArray As Double(,)
                massPrecisionArray = CType(massResolutionDataList, Double(,))
                dataCount = massPrecisionArray.GetLength(1)

                If dataCount > 0 Then
                    ReDim massResolutionData(dataCount - 1)

                    For i = 0 To dataCount - 1
                        Dim massPrecisionInfo As New udtMassPrecisionInfoType

                        With massPrecisionInfo
                            .Intensity = massPrecisionArray(0, i)
                            .Mass = massPrecisionArray(1, i)
                            .AccuracyMMU = massPrecisionArray(2, i)
                            .AccuracyPPM = massPrecisionArray(3, i)
                            .Resolution = massPrecisionArray(4, i)
                        End With

                        massResolutionData(i) = massPrecisionInfo
                    Next

                Else
                    ReDim massResolutionData(-1)
                End If

                Return dataCount

            Catch ex As AccessViolationException
                Dim strError As String = "Unable to load data for scan " & scan & "; possibly a corrupt .Raw file"
                RaiseWarningMessage(strError)

            Catch ex As Exception

                Dim strError As String = "Unable to load data for scan " & scan & ": " & ex.Message & "; possibly a corrupt .Raw file"
                RaiseErrorMessage(strError)

            End Try

            ReDim massResolutionData(-1)
            Return -1

        End Function

        '' GetNoiseData() returns data that isn't directly mappable to scan masses...
        '<System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        'Public Function GetScanNoiseData(ByVal scan As Integer, <Out()> ByRef noiseData() As udtNoisePackets) As Integer

        '    ' Note that we're using function attribute HandleProcessCorruptedStateExceptions
        '    ' to force .NET to properly catch critical errors thrown by the XRawfile DLL

        '    Dim dataCount As Integer

        '    If scan < mFileInfo.ScanStart Then
        '        scan = mFileInfo.ScanStart
        '    ElseIf scan > mFileInfo.ScanEnd Then
        '        scan = mFileInfo.ScanEnd
        '    End If

        '    Dim scanInfo As clsScanInfo = Nothing

        '    If Not GetScanInfo(scan, scanInfo) Then
        '        Throw New Exception("Cannot retrieve ScanInfo from cache for scan " & scan & "; cannot retrieve scan data")
        '    End If

        '    Try
        '        If mXRawFile Is Nothing Then
        '            Exit Try
        '        End If

        '    If Not scanInfo.IsFTMS Then
        '        Exit Try
        '    End If

        '        Dim NoiseDataList As Object = Nothing

        '        ' Returns double, float, float (mass, noise, baseline)
        '        mXRawFile.GetNoiseData(NoiseDataList, scan)

        '        Dim noiseDataArray As Double(,)
        '        noiseDataArray = CType(NoiseDataList, Double(,))
        '        dataCount = noiseDataArray.GetLength(1)

        '        If dataCount > 0 Then
        '            ReDim noiseData(dataCount - 1)

        '            For i = 0 To dataCount - 1
        '                Dim noisePacket As New udtNoisePackets

        '                With noisePacket
        '                    .Mass = noiseDataArray(0, i)
        '                    .Noise = CType(noiseDataArray(1, i), Single)
        '                    .Baseline = CType(noiseDataArray(2, i), Single)
        '                End With

        '                noiseData(i) = noisePacket
        '            Next

        '        Else
        '            ReDim noiseData(-1)
        '        End If

        '        Return dataCount

        '    Catch ex As AccessViolationException
        '        Dim strError As String = "Unable to load data for scan " & scan & "; possibly a corrupt .Raw file"
        '        RaiseWarningMessage(strError)

        '    Catch ex As Exception

        '        Dim strError As String = "Unable to load data for scan " & scan & ": " & ex.Message & "; possibly a corrupt .Raw file"
        '        RaiseErrorMessage(strError)

        '    End Try

        '    ReDim noiseData(-1)
        '    Return -1
        'End Function


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="scanFirst"></param>
        ''' <param name="scanLast"></param>
        ''' <param name="dblMassIntensityPairs"></param>
        ''' <param name="intMaxNumberOfPeaks"></param>
        ''' <param name="blnCentroid"></param>
        ''' <returns>The number of data points</returns>
        ''' <remarks></remarks>
        <System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()>
        Public Function GetScanDataSumScans(
            ByVal scanFirst As Integer,
            ByVal scanLast As Integer,
            <Out()> ByRef dblMassIntensityPairs(,) As Double,
            ByVal intMaxNumberOfPeaks As Integer,
            ByVal blnCentroid As Boolean) As Integer

            ' Note that we're using function attribute HandleProcessCorruptedStateExceptions
            ' to force .NET to properly catch critical errors thrown by the XRawfile DLL

            Dim dataCount As Integer

            Dim strFilter As String
            Dim intIntensityCutoffValue As Integer
            Dim intCentroidResult As Integer
            Dim dblCentroidPeakWidth As Double

            Dim MassIntensityPairsList As Object = Nothing
            Dim PeakList As Object = Nothing

            dataCount = 0

            Try
                If mXRawFile Is Nothing Then
                    Exit Try
                End If

                ' Make sure the MS controller is selected
                If Not SetMSController() Then
                    Exit Try
                End If

                If scanFirst < mFileInfo.ScanStart Then
                    scanFirst = mFileInfo.ScanStart
                ElseIf scanFirst > mFileInfo.ScanEnd Then
                    scanFirst = mFileInfo.ScanEnd
                End If

                If scanLast < scanFirst Then scanLast = scanFirst

                If scanLast < mFileInfo.ScanStart Then
                    scanLast = mFileInfo.ScanStart
                ElseIf scanLast > mFileInfo.ScanEnd Then
                    scanLast = mFileInfo.ScanEnd
                End If

                strFilter = String.Empty            ' Could use this to filter the data returned from the scan; must use one of the filters defined in the file (see .GetFilters())
                intIntensityCutoffValue = 0

                If intMaxNumberOfPeaks < 0 Then intMaxNumberOfPeaks = 0

                ' Warning: the masses reported by GetAverageMassList when centroiding are not properly calibrated and thus could be off by 0.3 m/z or more
                ' For an example, see function GetScanData2D above

                If blnCentroid Then
                    intCentroidResult = 1           ' Set to 1 to indicate that peaks should be centroided (only appropriate for profile data)
                Else
                    intCentroidResult = 0           ' Return the data as-is
                End If

                Dim backgroundScan1First = 0
                Dim backgroundScan1Last = 0
                Dim backgroundScan2First = 0
                Dim backgroundScan2Last = 0

                mXRawFile.GetAverageMassList(
                    scanFirst, scanLast,
                    backgroundScan1First,
                    backgroundScan1Last,
                    backgroundScan2First,
                    backgroundScan2Last,
                    strFilter,
                    IntensityCutoffTypeConstants.None,
                    intIntensityCutoffValue,
                    intMaxNumberOfPeaks,
                    intCentroidResult,
                    dblCentroidPeakWidth,
                    MassIntensityPairsList,
                    PeakList,
                    dataCount)

                If dataCount > 0 Then
                    dblMassIntensityPairs = CType(MassIntensityPairsList, Double(,))
                Else
                    ReDim dblMassIntensityPairs(-1, -1)
                End If

                Return dataCount

            Catch ex As AccessViolationException
                Dim strError As String = "Unable to load data summing scans " & scanFirst & " to " & scanLast & "; possibly a corrupt .Raw file"
                RaiseWarningMessage(strError)

            Catch ex As Exception

                Dim strError As String = "Unable to load data summing scans " & scanFirst & " to " & scanLast & ": " & ex.Message & "; possibly a corrupt .Raw file"
                RaiseErrorMessage(strError)

            End Try

            ReDim dblMassIntensityPairs(-1, -1)
            Return -1

        End Function

        Public Shared Sub InitializeMRMInfo(<Out()> ByRef udtMRMInfo As udtMRMInfoType, ByVal intInitialMassCountCapacity As Integer)

            If intInitialMassCountCapacity < 0 Then
                intInitialMassCountCapacity = 0
            End If

            udtMRMInfo = New udtMRMInfoType

            udtMRMInfo.MRMMassCount = 0
            ReDim udtMRMInfo.MRMMassList(intInitialMassCountCapacity - 1)

        End Sub

        Public Overrides Function OpenRawFile(ByVal FileName As String) As Boolean
            Dim intResult As Integer
            Dim blnSuccess As Boolean

            Try

                ' Make sure any existing open files are closed
                CloseRawFile()

                mCachedScanInfo.Clear()

                If mXRawFile Is Nothing Then
                    mXRawFile = CType(New MSFileReader_XRawfile, IXRawfile5)
                End If

                mXRawFile.Open(FileName)
                mXRawFile.IsError(intResult)        ' Unfortunately, .IsError() always returns 0, even if an error occurred

                If intResult = 0 Then
                    mCachedFileName = FileName
                    If FillFileInfo() Then
                        With mFileInfo
                            If .ScanStart = 0 AndAlso
                               .ScanEnd = 0 AndAlso
                               .VersionNumber = 0 AndAlso
                               Math.Abs(.MassResolution - 0) < Double.Epsilon AndAlso
                               .InstModel = Nothing Then

                                ' File actually didn't load correctly, since these shouldn't all be blank
                                blnSuccess = False
                            Else
                                blnSuccess = True
                            End If
                        End With
                    Else
                        blnSuccess = False
                    End If
                Else
                    blnSuccess = False
                End If

            Catch ex As Exception
                blnSuccess = False
            Finally
                If Not blnSuccess Then
                    mCachedFileName = String.Empty
                End If
            End Try

            Return blnSuccess

        End Function

        Private Function TuneMethodsMatch(ByVal udtMethod1 As udtTuneMethodType, ByVal udtMethod2 As udtTuneMethodType) As Boolean
            Dim blnMatch As Boolean
            Dim intIndex As Integer

            blnMatch = True


            If udtMethod1.Count <> udtMethod2.Count Then
                ' Different segment number of setting count; the methods don't match
                blnMatch = False
            Else
                For intIndex = 0 To udtMethod1.Count - 1
                    If udtMethod1.SettingCategory(intIndex) <> udtMethod2.SettingCategory(intIndex) OrElse
                       udtMethod1.SettingName(intIndex) <> udtMethod2.SettingName(intIndex) OrElse
                       udtMethod1.SettingValue(intIndex) <> udtMethod2.SettingValue(intIndex) Then
                        ' Different segment data; the methods don't match
                        blnMatch = False
                        Exit For
                    End If
                Next intIndex
            End If


            Return blnMatch

        End Function

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            CloseRawFile()

            mCachedScanInfo = New Dictionary(Of Integer, clsScanInfo)
        End Sub

    End Class
End Namespace
