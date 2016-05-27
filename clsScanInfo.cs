Imports System.Linq
Imports System.Runtime.InteropServices
Imports ThermoRawFileReaderDLL.FinniganFileIO

Public Class clsScanInfo

#Region "Member variables"

    Protected ReadOnly mCacheDateUTC As DateTime
    Protected ReadOnly mScanNumber As Integer

    Protected mFilterString As String
    Protected ReadOnly mScanEvents As List(Of KeyValuePair(Of String, String))
    Protected ReadOnly mStatusLog As List(Of KeyValuePair(Of String, String))

#End Region

#Region "Properties"

    ''' <summary>
    ''' UTC Time that this scan info was cached
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Used for determining which cached scan info can be discarded if too many scans become cached</remarks>
    Public ReadOnly Property CacheDateUTC As DateTime
        Get
            Return mCacheDateUTC
        End Get
    End Property

    ''' <summary>
    ''' Scan number
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ScanNumber As Integer
        Get
            Return mScanNumber
        End Get
    End Property

    ''' <summary>
    ''' MS Level
    ''' </summary>
    ''' <value></value>
    ''' <returns>MS acquisition level, where 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS</returns>
    ''' <remarks></remarks>
    Public Property MSLevel As Integer

    ''' <summary>
    ''' Event Number
    ''' </summary>
    ''' <value></value>
    ''' <returns>1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.</returns>
    ''' <remarks></remarks>
    Public Property EventNumber As Integer

    ''' <summary>
    ''' SIM Scan Flag
    ''' </summary>
    ''' <value></value>
    ''' <returns>True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined)</returns>
    ''' <remarks>If multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS</remarks>
    Public Property SIMScan As Boolean

    ''' <summary>
    ''' Multiple reaction monitoring mode
    ''' </summary>
    ''' <value></value>
    ''' <returns>1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)</returns>
    ''' <remarks></remarks>
    Public Property MRMScanType As FinniganFileReaderBaseClass.MRMScanTypeConstants

    ''' <summary>
    ''' Zoom scan flag
    ''' </summary>
    ''' <value></value>
    ''' <returns>True when the given scan is a zoomed in mass region</returns>
    ''' <remarks>These spectra are typically skipped when creating SICs</remarks>
    Public Property ZoomScan As Boolean

    ''' <summary>
    ''' Number of mass intensity value pairs
    ''' </summary>
    ''' <value></value>
    ''' <returns>Number of points, -1 if unknown</returns>
    ''' <remarks></remarks>
    Public Property NumPeaks As Integer

    ''' <summary>
    ''' Retention time (in minutes)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property RetentionTime As Double

    ''' <summary>
    ''' Lowest m/z value
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property LowMass As Double

    ''' <summary>
    ''' Highest m/z value
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property HighMass As Double

    ''' <summary>
    ''' Total ion current
    ''' </summary>
    ''' <value></value>
    ''' <returns>Sum of all ion abundances</returns>
    ''' <remarks></remarks>
    Public Property TotalIonCurrent As Double

    ''' <summary>
    ''' Base peak m/z
    ''' </summary>
    ''' <value></value>
    ''' <returns>m/z value of the most abundant ion in the scan</returns>
    ''' <remarks></remarks>
    Public Property BasePeakMZ As Double

    ''' <summary>
    ''' Base peak intensity
    ''' </summary>
    ''' <value></value>
    ''' <returns>Intensity of the most abundant ion in the scan</returns>
    ''' <remarks></remarks>
    Public Property BasePeakIntensity As Double

    ''' <summary>
    ''' Scan Filter string
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property FilterText As String

    ''' <summary>
    ''' Parent ion m/z
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ParentIonMZ As Double

    ''' <summary>
    ''' Activation type (aka activation method) as reported by the reader
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property ActivationType As FinniganFileReaderBaseClass.ActivationTypeConstants

    ''' <summary>
    ''' Collision mode, determined from the filter string
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Typically CID, ETD, HCD, EThcD, or ETciD</remarks>
    Public Property CollisionMode As String

    ''' <summary>
    ''' Ionization polarity
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IonMode As FinniganFileReaderBaseClass.IonModeConstants

    ''' <summary>
    ''' MRM mode
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property MRMInfo As FinniganFileReaderBaseClass.udtMRMInfoType

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property NumChannels As Integer

    ''' <summary>
    ''' Indicates whether the sampling time increment for the controller is constant
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property UniformTime As Boolean

    ''' <summary>
    ''' Sampling frequency for the current controller
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Frequency As Double

    ''' <summary>
    ''' Centroid scan flag
    ''' </summary>
    ''' <value></value>
    ''' <returns>True if centroid (sticks) scan; False if profile (continuum) scan</returns>
    ''' <remarks></remarks>
    Public Property IsCentroided As Boolean

    ''' <summary>
    ''' FTMS flag
    ''' </summary>
    ''' <value></value>
    ''' <returns>True if acquired on a high resolution mass analyzer (for example, on an Orbitrap or Q-Exactive)</returns>
    ''' <remarks></remarks>
    Public Property IsFTMS As Boolean

    ''' <summary>
    ''' Scan event data
    ''' </summary>
    ''' <value></value>
    ''' <returns>List of key/value pairs</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ScanEvents As List(Of KeyValuePair(Of String, String))
        Get
            Return mScanEvents
        End Get
    End Property

    ''' <summary>
    ''' Status log data
    ''' </summary>
    ''' <value></value>
    ''' <returns>List of key/value pairs</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property StatusLog As List(Of KeyValuePair(Of String, String))
        Get
            Return mStatusLog
        End Get
    End Property
#End Region

#Region "Constructor and public methods"

    ''' <summary>
    ''' Constructor with only scan number
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New(scan As Integer)
        NumPeaks = -1
        mScanNumber = scan
        mCacheDateUTC = DateTime.UtcNow

        mFilterString = String.Empty
        CollisionMode = String.Empty
        ActivationType = FinniganFileReaderBaseClass.ActivationTypeConstants.Unknown

        mScanEvents = New List(Of KeyValuePair(Of String, String))
        mStatusLog = New List(Of KeyValuePair(Of String, String))

    End Sub

    ''' <summary>
    ''' Constructor with scan number and data in a udtScanHeaderInfoType struct
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New(scan As Integer, udtScanHeaderInfo As FinniganFileReaderBaseClass.udtScanHeaderInfoType)
        Me.New(scan)

        CopyFromStruct(udtScanHeaderInfo)
    End Sub

    ''' <summary>
    ''' Store this scan's scan events using a parallel string arrays
    ''' </summary>
    ''' <param name="eventNames"></param>
    ''' <param name="eventValues"></param>
    ''' <remarks></remarks>
    Public Sub StoreScanEvents(eventNames() As String, eventValues() As String)
        StoreParallelStrings(mScanEvents, eventNames, eventValues)
    End Sub

    ''' <summary>
    ''' Store this scan's scan events using a parallel string arrays
    ''' </summary>
    ''' <param name="logNames"></param>
    ''' <param name="logValues"></param>
    ''' <remarks></remarks>
    Public Sub StoreStatusLog(logNames() As String, logValues() As String)
        StoreParallelStrings(mStatusLog, logNames, logValues)
    End Sub

    ''' <summary>
    ''' Get the event value associated with the given scan event name
    ''' </summary>
    ''' <param name="eventName">Event name to find</param>
    ''' <param name="eventValue">Event value</param>
    ''' <param name="partialMatchToStart">Set to true to match the start of an event name, and not require a full match</param>
    ''' <returns>True if found a match for the event name, otherwise false</returns>
    ''' <remarks>Event names nearly always end in a colon, e.g. "Monoisotopic M/Z:" or "Charge State:"</remarks>
    Public Function TryGetScanEvent(eventName As String, <Out()> ByRef eventValue As String, Optional partialMatchToStart As Boolean = False) As Boolean

        Dim lstResults As IEnumerable(Of KeyValuePair(Of String, String))

        If partialMatchToStart Then
            ' Partial match
            lstResults = From item In mScanEvents Where item.Key.ToLower().StartsWith(eventName.ToLower()) Select item
        Else
            lstResults = From item In mScanEvents Where String.Equals(item.Key, eventName, StringComparison.InvariantCultureIgnoreCase) Select item
        End If

        For Each item In lstResults
            eventValue = item.Value
            Return True
        Next

        eventValue = String.Empty
        Return False

    End Function


    Public Overrides Function ToString() As String
        If String.IsNullOrEmpty(FilterText) Then
            Return "Scan " & ScanNumber & ": Generic ScanHeaderInfo"
        Else
            Return "Scan " & ScanNumber & ": " & FilterText
        End If
    End Function

#End Region

#Region "Private methods"

    Private Sub CopyFromStruct(udtScanHeaderInfoType As FinniganFileReaderBaseClass.udtScanHeaderInfoType)

        With udtScanHeaderInfoType
            MSLevel = .MSLevel
            EventNumber = .EventNumber
            SIMScan = .SIMScan
            MRMScanType = .MRMScanType
            ZoomScan = .ZoomScan

            NumPeaks = .NumPeaks
            RetentionTime = .RetentionTime
            LowMass = .LowMass
            HighMass = .HighMass
            TotalIonCurrent = .TotalIonCurrent
            BasePeakMZ = .BasePeakMZ
            BasePeakIntensity = .BasePeakIntensity

            FilterText = .FilterText
            ParentIonMZ = .ParentIonMZ
            CollisionMode = .CollisionMode
            ActivationType = .ActivationType

            IonMode = .IonMode
            MRMInfo = .MRMInfo

            NumChannels = .NumChannels
            UniformTime = .UniformTime
            Frequency = .Frequency
            IsCentroided = .IsCentroidScan

            StoreScanEvents(.ScanEventNames, .ScanEventValues)
            StoreStatusLog(.StatusLogNames, .StatusLogValues)

        End With
    End Sub

    Private Sub StoreParallelStrings(
       targetList As ICollection(Of KeyValuePair(Of String, String)),
       names As IList(Of String),
       values As IList(Of String))

        targetList.Clear()

        For i = 0 To names.Count - 1
            targetList.Add(New KeyValuePair(Of String, String)(names(i), values(i)))
        Next

    End Sub
#End Region
End Class
