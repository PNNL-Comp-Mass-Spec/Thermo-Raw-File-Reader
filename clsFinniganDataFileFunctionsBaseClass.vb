Option Strict On

Imports System.Runtime.InteropServices

<Assembly: CLSCompliant(True)> 

' Base class for derived classes that can read Finnigan .Raw files (LCQ, LTQ, etc.)
' 
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

Namespace FinniganFileIO

    Public MustInherit Class FinniganFileReaderBaseClass

#Region "Constants and Enums"

        Public Enum ActivationTypeConstants
            Unknown = -1
            CID = 0
            MPD = 1
            ECD = 2
            PQD = 3
            ETD = 4
            HCD = 5
            AnyType = 6
            SA = 7
            PTR = 8
            NETD = 9
            NPTR = 10
        End Enum

        Public Enum MRMScanTypeConstants
            NotMRM = 0
            MRMQMS = 1              ' Multiple SIM ranges in a single scan
            SRM = 2                 ' Monitoring a parent ion and one or more daughter ions
            FullNL = 3              ' Full neutral loss scan
        End Enum

        Public Enum IonModeConstants
            Unknown = 0
            Positive = 1
            Negative = 2
        End Enum

        Protected MAX_SCANS_TO_CACHE_INFO As Integer = 50000

#End Region

#Region "Structures"

        Public Structure udtTuneMethodType
            Public Count As Integer
            Public SettingCategory() As String
            Public SettingName() As String
            Public SettingValue() As String
        End Structure

        Public Structure udtFileInfoType
            Public AcquisitionDate As String        ' Will often be blank
            Public AcquisitionFilename As String    ' Will often be blank
            Public Comment1 As String               ' Will often be blank
            Public Comment2 As String               ' Will often be blank
            Public SampleName As String             ' Will often be blank
            Public SampleComment As String          ' Will often be blank

            Public CreationDate As DateTime
            Public CreatorID As String              ' Logon name of the user when the file was created
            Public InstFlags As String              ' Values should be one of the constants in InstFlags
            Public InstHardwareVersion As String
            Public InstSoftwareVersion As String
            Public InstMethods() As String          ' Typically only have one instrument method; the length of this array defines the number of instrument methods
            Public InstModel As String
            Public InstName As String
            Public InstrumentDescription As String  ' Typically only defined for instruments converted from other formats
            Public InstSerialNumber As String
            Public TuneMethods() As udtTuneMethodType   ' Typically have one or two tune methods; the length of this array defines the number of tune methods defined
            Public VersionNumber As Integer         ' File format Version Number
            Public MassResolution As Double
            Public ScanStart As Integer
            Public ScanEnd As Integer
        End Structure

        Public Structure udtMRMMassRangeType
            Public StartMass As Double
            Public EndMass As Double
            Public CentralMass As Double        ' Useful for MRM/SRM experiments

            Public Overrides Function ToString() As String
                Return StartMass.ToString("0.000") & "-" & EndMass.ToString("0.000")
            End Function
        End Structure

        Public Structure udtMRMInfoType
            Public MRMMassCount As Integer                  ' List of mass ranges monitored by the first quadrupole
            Public MRMMassList() As udtMRMMassRangeType
        End Structure

        Public Structure udtScanHeaderInfoType
            Public MSLevel As Integer                   ' 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS
            Public EventNumber As Integer               ' 1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.
            Public SIMScan As Boolean                   ' True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined); if multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS
            Public MRMScanType As MRMScanTypeConstants  ' 1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)
            Public ZoomScan As Boolean                  ' True when the given scan is a zoomed in mass region; these spectra are typically skipped when creating SICs

            Public NumPeaks As Integer                  ' Number of mass intensity value pairs in the specified scan (may not be defined until .GetScanData() is called; -1 if unknown)
            Public RetentionTime As Double              ' Retention time (in minutes)
            Public LowMass As Double
            Public HighMass As Double
            Public TotalIonCurrent As Double
            Public BasePeakMZ As Double
            Public BasePeakIntensity As Double

            Public FilterText As String
            Public ParentIonMZ As Double

            Public ActivationType As ActivationTypeConstants    ' Activation type (aka activation method) as reported by the reader
            Public CollisionMode As String                      ' Activation type, determined from the filter string

            Public IonMode As IonModeConstants
            Public MRMInfo As udtMRMInfoType

            Public NumChannels As Integer
            Public UniformTime As Boolean               ' Indicates whether the sampling time increment for the controller is constant
            Public Frequency As Double                  ' Sampling frequency for the current controller
            Public IsCentroidScan As Boolean            ' True if centroid (sticks) scan; False if profile (continuum) scan

            Public ScanEventNames() As String
            Public ScanEventValues() As String

            Public StatusLogNames() As String
            Public StatusLogValues() As String

            Public Overrides Function ToString() As String
                If String.IsNullOrEmpty(FilterText) Then
                    Return "Generic udtScanHeaderInfoType"
                Else
                    Return FilterText
                End If
            End Function
        End Structure

#End Region

#Region "Classwide Variables"

        Protected mCachedFileName As String

        Protected mCachedScanInfo As Dictionary(Of Integer, clsScanInfo)

        Protected mFileInfo As udtFileInfoType

        Protected mLoadMSMethodInfo As Boolean = True
        Protected mLoadMSTuneInfo As Boolean = True

#End Region

#Region "Interface Functions"

        Public ReadOnly Property FileInfo() As udtFileInfoType
            Get
                Return mFileInfo
            End Get
        End Property

        Public Property LoadMSMethodInfo() As Boolean
            Get
                Return mLoadMSMethodInfo
            End Get
            Set(value As Boolean)
                mLoadMSMethodInfo = value
            End Set
        End Property

        Public Property LoadMSTuneInfo() As Boolean
            Get
                Return mLoadMSTuneInfo
            End Get
            Set(value As Boolean)
                mLoadMSTuneInfo = value
            End Set
        End Property

#End Region

#Region "Events"

        Public Event ReportError(strMessage As String)
        Public Event ReportWarning(strMessage As String)

#End Region

        Public MustOverride Function CheckFunctionality() As Boolean
        Public MustOverride Sub CloseRawFile()
        Public MustOverride Function GetNumScans() As Integer

        Public MustOverride Function GetScanInfo(Scan As Integer, ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Boolean
        Public MustOverride Function GetScanInfo(Scan As Integer, ByRef scanInfo As clsScanInfo) As Boolean

        Public MustOverride Overloads Function GetScanData(Scan As Integer, <Out()> ByRef dblIonMZ() As Double, <Out()> ByRef dblIonIntensity() As Double) As Integer
        Public MustOverride Overloads Function GetScanData(Scan As Integer, <Out()> ByRef dblIonMZ() As Double, <Out()> ByRef dblIonIntensity() As Double, intMaxNumberOfPeaks As Integer) As Integer

        Public MustOverride Function OpenRawFile(FileName As String) As Boolean

        Protected MustOverride Function FillFileInfo() As Boolean

        Public Shared Sub DuplicateMRMInfo(ByRef udtSource As udtMRMInfoType, ByRef udtTarget As udtMRMInfoType)
            With udtSource
                udtTarget.MRMMassCount = .MRMMassCount

                If .MRMMassList Is Nothing Then
                    ReDim udtTarget.MRMMassList(-1)
                Else
                    ReDim udtTarget.MRMMassList(.MRMMassList.Length - 1)
                    Array.Copy(.MRMMassList, udtTarget.MRMMassList, .MRMMassList.Length)
                End If
            End With
        End Sub

        Protected Sub RaiseErrorMessage(strMessage As String)
            RaiseEvent ReportError(strMessage)
        End Sub

        Protected Sub RaiseWarningMessage(strMessage As String)
            RaiseEvent ReportWarning(strMessage)
        End Sub
    End Class
End Namespace