Option Strict On

Imports System.Reflection
Imports ThermoRawFileReaderDLL
Imports ThermoRawFileReaderDLL.FinniganFileIO

Module modMain

    Private Const DEFAULT_FILE_PATH As String = "..\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW"

    Public Sub Main()

        Dim commandLineParser = New clsParseCommandLine()
        commandLineParser.ParseCommandLine()

        If commandLineParser.NeedToShowHelp Then
            ShowProgramHelp()
            Exit Sub
        End If

        Dim sourceFilePath = DEFAULT_FILE_PATH

        If commandLineParser.NonSwitchParameterCount > 0 Then
            sourceFilePath = commandLineParser.RetrieveNonSwitchParameter(0)
        End If

        Dim fiSourceFile = New IO.FileInfo(sourceFilePath)

        If Not fiSourceFile.Exists Then
            Console.WriteLine("File not found: " + fiSourceFile.FullName)
            Exit Sub
        End If

        Dim centroid = commandLineParser.IsParameterPresent("centroid")
        Dim testSumming = commandLineParser.IsParameterPresent("sum")

        Dim startScan = 0
        Dim endScan = 0

        Dim strValue As String = String.Empty
        Dim intValue As Integer

        If commandLineParser.RetrieveValueForParameter("Start", strValue) Then
            If Integer.TryParse(strValue, intValue) Then
                startScan = intValue
            End If
        End If

        If commandLineParser.RetrieveValueForParameter("End", strValue) Then
            If Integer.TryParse(strValue, intValue) Then
                endScan = intValue
            End If
        End If

        TestScanFilterParsing()

        TestReader(fiSourceFile.FullName, centroid, testSumming, startScan, endScan)

        If centroid Then
            ' Also process the file with centroiding off
            TestReader(fiSourceFile.FullName, False, testSumming, startScan, endScan)
        End If

        ' Uncomment the following to test the GetCollisionEnergy() function
        'TestReader("..\EDRN_ERG_Spop_ETV1_50fmolHeavy_0p5ugB53A_Frac48_3Oct12_Gandalf_W33A1_16a.raw")

        Console.WriteLine("Done")

    End Sub

    Private Sub ShowProgramHelp()

        Dim assemblyNameLocation = Assembly.GetExecutingAssembly().Location

        Console.WriteLine("Program syntax:" & Environment.NewLine & IO.Path.GetFileName(assemblyNameLocation))
        Console.WriteLine(" InputFilePath.raw [/Centroid] [/Sum] [/Start:Scan] [/End:Scan]")

        Console.WriteLine("Running this program without any parameters it will process file " + DEFAULT_FILE_PATH)
        Console.WriteLine()
        Console.WriteLine("The first parameter specifies the file to read")
        Console.WriteLine()
        Console.WriteLine("Use /Centroid to centroid the data when reading")
        Console.WriteLine("Use /Sum to test summing the data across 15 scans (each spectrum will be shown twice; once with summing and once without)")
        Console.WriteLine()
        Console.WriteLine("Use /Start and /End to limit the scan range to process")
        Console.WriteLine("If /Start and /End are not provided, then will read every 21 scans")

    End Sub

    Private Sub TestReader(
      ByVal rawFilePath As String,
      Optional ByVal centroid As Boolean = False,
      Optional ByVal testSumming As Boolean = False,
      Optional ByVal scanStart As Integer = 0,
      Optional ByVal scanEnd As Integer = 0)

        Try
            If Not IO.File.Exists(rawFilePath) Then
                Console.WriteLine("File not found, skipping: " & rawFilePath)
                Exit Sub
            End If

            Using oReader = New XRawFileIO(rawFilePath)

                ' ReSharper disable once UseImplicitlyTypedVariableEvident
                For intIndex As Integer = 0 To oReader.FileInfo.InstMethods.Length - 1
                    Console.WriteLine(oReader.FileInfo.InstMethods(intIndex))
                Next

                Dim iNumScans = oReader.GetNumScans()

                Dim bSuccess As Boolean

                Dim dblMzList() As Double
                Dim dblIntensityList() As Double
                Dim dblMassIntensityPairs As Double(,)

                Dim lstCollisionEnergies As List(Of Double)
                Dim strCollisionEnergies As String = String.Empty

                ReDim dblMzList(0)
                ReDim dblIntensityList(0)
                ReDim dblMassIntensityPairs(0, 0)

                ShowMethod(oReader)

                Dim scanStep = 1

                If scanStart < 1 Then scanStart = 1
                If scanEnd < 1 Then
                    scanEnd = iNumScans
                    scanStep = 21
                Else
                    If scanEnd < scanStart Then
                        scanEnd = scanStart
                    End If
                End If

                For iScanNum As Integer = scanStart To scanEnd Step scanStep

                    Dim oScanInfo As clsScanInfo = Nothing

                    bSuccess = oReader.GetScanInfo(iScanNum, oScanInfo)
                    If bSuccess Then
                        Console.Write("Scan " & iScanNum & " at " & oScanInfo.RetentionTime.ToString("0.00") & " minutes: " & oScanInfo.FilterText)
                        lstCollisionEnergies = oReader.GetCollisionEnergy(iScanNum)

                        If lstCollisionEnergies.Count = 0 Then
                            strCollisionEnergies = String.Empty
                        ElseIf lstCollisionEnergies.Count >= 1 Then
                            strCollisionEnergies = lstCollisionEnergies.Item(0).ToString("0.0")

                            If lstCollisionEnergies.Count > 1 Then
                                For intIndex = 1 To lstCollisionEnergies.Count - 1
                                    strCollisionEnergies &= ", " & lstCollisionEnergies.Item(intIndex).ToString("0.0")
                                Next
                            End If
                        End If

                        If String.IsNullOrEmpty(strCollisionEnergies) Then
                            Console.WriteLine()
                        Else
                            Console.WriteLine("; CE " & strCollisionEnergies)
                        End If

                        Dim monoMZ As String = String.Empty
                        Dim chargeState As String = String.Empty
                        Dim isolationWidth As String = String.Empty

                        If oScanInfo.TryGetScanEvent("Monoisotopic M/Z:", monoMZ, False) Then
                            Console.WriteLine("Monoisotopic M/Z: " + monoMZ)
                        End If

                        If oScanInfo.TryGetScanEvent("Charge State", chargeState, True) Then
                            Console.WriteLine("Charge State: " + chargeState)
                        End If

                        If oScanInfo.TryGetScanEvent("MS2 Isolation Width", isolationWidth, True) Then
                            Console.WriteLine("MS2 Isolation Width: " + isolationWidth)
                        End If

                        If iScanNum Mod 50 = 0 OrElse scanEnd - scanStart <= 50 Then
                            ' Get the data for scan iScanNum

                            Console.WriteLine()
                            Console.WriteLine("Spectrum for scan " & iScanNum)
                            Dim intDataCount = oReader.GetScanData(iScanNum, dblMzList, dblIntensityList, 0, centroid)

                            Dim mzDisplayStepSize = 50
                            If centroid Then
                                mzDisplayStepSize = 1
                            End If

                            ' ReSharper disable once UseImplicitlyTypedVariableEvident
                            For iDataPoint As Integer = 0 To dblMzList.Length - 1 Step mzDisplayStepSize
                                Console.WriteLine("  " & dblMzList(iDataPoint).ToString("0.000") & " mz   " & dblIntensityList(iDataPoint).ToString("0"))
                            Next
                            Console.WriteLine()

                            Const scansToSum = 15
                            If iScanNum + scansToSum < iNumScans And testSumming Then

                                ' Get the data for scan iScanNum through iScanNum + 15
                                Dim dataCount = oReader.GetScanDataSumScans(iScanNum, iScanNum + scansToSum, dblMassIntensityPairs, 0, centroid)

                                Console.WriteLine("Summed spectrum, scans " & iScanNum & " through " & (iScanNum + scansToSum).ToString())

                                ' ReSharper disable once UseImplicitlyTypedVariableEvident
                                For iDataPoint As Integer = 0 To dblMassIntensityPairs.GetLength(1) - 1 Step 50
                                    Console.WriteLine("  " & dblMassIntensityPairs(0, iDataPoint).ToString("0.000") & " mz   " & dblMassIntensityPairs(1, iDataPoint).ToString("0"))
                                Next

                                Console.WriteLine()
                            End If

                            If oScanInfo.IsFTMS Then
                                Dim ftLabelData As XRawFileIO.udtFTLabelInfoType() = Nothing

                                Dim dataCount = oReader.GetScanLabelData(iScanNum, ftLabelData)

                                Console.WriteLine()
                                Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,12}", "Mass", "Intensity", "Resolution", "Baseline", "Noise", "Charge")

                                For iDataPoint = 0 To dataCount - 1 Step 50

                                    Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,12}",
                                                      ftLabelData(iDataPoint).Mass.ToString("0.000"),
                                                      ftLabelData(iDataPoint).Intensity.ToString("0"),
                                                      ftLabelData(iDataPoint).Resolution.ToString("0"),
                                                      ftLabelData(iDataPoint).Baseline.ToString("0.0"),
                                                      ftLabelData(iDataPoint).Noise.ToString("0"),
                                                      ftLabelData(iDataPoint).Charge.ToString("0")
                                                      )
                                Next

                                Dim ftPrecisionData As XRawFileIO.udtMassPrecisionInfoType() = Nothing

                                dataCount = oReader.GetScanPrecisionData(iScanNum, ftPrecisionData)

                                Console.WriteLine()
                                Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}", "Mass", "Intensity", "AccuracyMMU", "AccuracyPPM", "Resolution")

                                For iDataPoint = 0 To dataCount - 1 Step 50

                                    Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}",
                                                      ftPrecisionData(iDataPoint).Mass.ToString("0.000"),
                                                      ftPrecisionData(iDataPoint).Intensity.ToString("0"),
                                                      ftPrecisionData(iDataPoint).AccuracyMMU.ToString("0.000"),
                                                      ftPrecisionData(iDataPoint).AccuracyPPM.ToString("0.000"),
                                                      ftPrecisionData(iDataPoint).Resolution.ToString("0")
                                                      )
                                Next
                            End If

                        End If

                    End If
                Next

            End Using


        Catch ex As Exception
            Console.WriteLine("Error in sub TestReader: " & ex.Message)
        End Try
    End Sub

    Private Sub TestScanFilterParsing()

        ' Note: See also class ThermoReaderUnitTests in the RawFileReaderUnitTests project

        Dim filterList = New List(Of String)

        filterList.Add("ITMS + c ESI Full ms [300.00-2000.00]")
        filterList.Add("FTMS + p NSI Full ms [400.00-2000.00]")
        filterList.Add("ITMS + p ESI d Z ms [579.00-589.00]")
        filterList.Add("ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]")
        filterList.Add("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]")
        filterList.Add("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]")
        filterList.Add("ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]")
        filterList.Add("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]")
        filterList.Add("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]")
        filterList.Add("ITMS + c NSI d Full ms10 421.76@35.00")
        filterList.Add("+ p ms2 777.00@cid30.00 [210.00-1200.00]")
        filterList.Add("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]")
        filterList.Add("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]")
        filterList.Add("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]")
        filterList.Add("+ p NSI Q3MS [150.070-1500.000]")
        filterList.Add("c NSI Full cnl 162.053 [300.000-1200.000]")
        filterList.Add("- p NSI Full ms2 168.070 [300.000-1500.00]")
        filterList.Add("+ c NSI Full ms2 1083.000 [300.000-1500.00]")
        filterList.Add("- p NSI Full ms2 247.060 [300.000-1500.00]")
        filterList.Add("- c NSI d Full ms2 921.597 [300.000-1500.00]")
        filterList.Add("+ c NSI SRM ms2 965.958 [300.000-1500.00]")
        filterList.Add("+ p NSI SRM ms2 1025.250 [300.000-1500.00]")
        filterList.Add("+ c EI SRM ms2 247.000 [300.000-1500.00]")
        filterList.Add("+ p NSI Full ms2 589.840 [300.070-1200.000]")
        filterList.Add("+ p NSI ms [0.316-316.000]")

        For Each filterItem In filterList
            Dim genericFilter = XRawFileIO.MakeGenericFinniganScanFilter(filterItem)
            Dim scanType = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(filterItem)

            Console.WriteLine(filterItem)
            Console.WriteLine("  {0,-12} {1}", scanType, genericFilter)
            Console.WriteLine()
        Next

        Console.WriteLine()

    End Sub


    Private Function ShowMethod(ByVal oReader As XRawFileIO) As Boolean
        Dim intInstMethodCount As Integer
        Dim strMethodNum As String

        Try
            intInstMethodCount = oReader.FileInfo.InstMethods.Length
        Catch ex As Exception
            Return False
        End Try

        Try
            For intIndex = 0 To intInstMethodCount - 1
                If intIndex = 0 And oReader.FileInfo.InstMethods.Length = 1 Then
                    strMethodNum = String.Empty
                Else
                    strMethodNum = (intIndex + 1).ToString.Trim
                End If

                With oReader.FileInfo
                    Console.WriteLine("Instrument model: " & .InstModel)
                    Console.WriteLine("Instrument name: " & .InstName)
                    Console.WriteLine("Instrument description: " & .InstrumentDescription)
                    Console.WriteLine("Instrument serial number: " & .InstSerialNumber)
                    Console.WriteLine()

                    Console.WriteLine(oReader.FileInfo.InstMethods(intIndex))
                End With


            Next intIndex

        Catch ex As Exception
            Console.WriteLine("Error loading the MS Method: " & ex.Message)
            Return False
        End Try

        Return True
    End Function

End Module
