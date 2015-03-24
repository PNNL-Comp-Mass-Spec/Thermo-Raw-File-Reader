Option Strict On

Imports System.Reflection
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

        TestReader(fiSourceFile.FullName, centroid, startScan, endScan)

        If centroid Then
            ' Also process the file with centroiding off
            TestReader(fiSourceFile.FullName, False, startScan, endScan)
        End If

        ' Uncomment the following to test the GetCollisionEnergy() function
        'TestReader("..\EDRN_ERG_Spop_ETV1_50fmolHeavy_0p5ugB53A_Frac48_3Oct12_Gandalf_W33A1_16a.raw")

        Console.WriteLine("Done")

    End Sub


    Private Sub ShowProgramHelp()

        Dim assemblyNameLocation = Assembly.GetExecutingAssembly().Location

        Console.WriteLine("Program syntax:" & Environment.NewLine & IO.Path.GetFileName(assemblyNameLocation))
        Console.WriteLine(" InputFilePath.raw [/Centroid] [/Start:Scan] [/End:Scan]")

        Console.WriteLine("Running this program without any parameters it will process file " + DEFAULT_FILE_PATH)
        Console.WriteLine()
        Console.WriteLine("The first parameter specifies the file to read")
        Console.WriteLine()
        Console.WriteLine("Use /Centroid to centroid the data when reading")
        Console.WriteLine()
        Console.WriteLine("Use /Start and /End to limit the scan range to process")
        Console.WriteLine("If /Start and /End are not provided, then will read every 21 scans")

    End Sub

    Private Sub TestReader(
      ByVal strRawFilePath As String,
      Optional ByVal blnCentroid As Boolean = False,
      Optional ByVal scanStart As Integer = 0,
      Optional ByVal scanEnd As Integer = 0)

        Try
            If Not IO.File.Exists(strRawFilePath) Then
                Console.WriteLine("File not found, skipping: " & strRawFilePath)
                Exit Sub
            End If

            Dim oReader = New XRawFileIO()

            oReader.OpenRawFile(strRawFilePath)

            For intIndex As Integer = 0 To oReader.FileInfo.InstMethods.Length - 1
                Console.WriteLine(oReader.FileInfo.InstMethods(intIndex))
            Next

            Dim iNumScans = oReader.GetNumScans()

            Dim udtScanHeaderInfo As FinniganFileReaderBaseClass.udtScanHeaderInfoType
            Dim bSuccess As Boolean
            Dim intDataCount As Integer

            Dim dblMzList() As Double
            Dim dblIntensityList() As Double
            Dim dblMassIntensityPairs As Double(,)

            Dim lstCollisionEnergies As List(Of Double)
            Dim strCollisionEnergies As String = String.Empty

            ReDim dblMzList(0)
            ReDim dblIntensityList(0)
            ReDim dblMassIntensityPairs(0, 0)

            udtScanHeaderInfo = New FinniganFileReaderBaseClass.udtScanHeaderInfoType

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

                bSuccess = oReader.GetScanInfo(iScanNum, udtScanHeaderInfo)
                If bSuccess Then
                    Console.Write("Scan " & iScanNum & " at " & udtScanHeaderInfo.RetentionTime.ToString("0.00") & " minutes: " & udtScanHeaderInfo.FilterText)
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

                    If iScanNum Mod 50 = 0 OrElse scanEnd - scanStart <= 50 Then
                        ' Get the data for scan iScanNum

                        Console.WriteLine()
                        Console.WriteLine("Spectrum for scan " & iScanNum)
                        intDataCount = oReader.GetScanData(iScanNum, dblMzList, dblIntensityList, 0, blnCentroid)

                        Dim mzDisplayStepSize = 50
                        If blnCentroid Then
                            mzDisplayStepSize = 1
                        End If

                        For iDataPoint As Integer = 0 To dblMzList.Length - 1 Step mzDisplayStepSize
                            Console.WriteLine("  " & dblMzList(iDataPoint).ToString("0.000") & " mz   " & dblIntensityList(iDataPoint).ToString("0"))
                        Next
                        Console.WriteLine()

                        Const scansToSum As Integer = 15
                        If iScanNum + scansToSum < iNumScans Then

                            ' Get the data for scan iScanNum through iScanNum + 15
                            oReader.GetScanDataSumScans(iScanNum, iScanNum + scansToSum, dblMassIntensityPairs, 0, blnCentroid)

                            Console.WriteLine("Summed spectrum, scans " & iScanNum & " through " & (iScanNum + scansToSum).ToString())

                            For iDataPoint As Integer = 0 To dblMassIntensityPairs.GetLength(1) - 1 Step 50
                                Console.WriteLine("  " & dblMassIntensityPairs(0, iDataPoint).ToString("0.000") & " mz   " & dblMassIntensityPairs(1, iDataPoint).ToString("0"))
                            Next

                            Console.WriteLine()
                        End If


                    End If

                End If
            Next

            oReader.CloseRawFile()

        Catch ex As Exception
            Console.WriteLine("Error in sub TestReader: " & ex.Message)
        End Try
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
	
	Private Class clsMzListComparer
		Inherits Generic.Comparer(Of KeyValuePair(Of Double, Double))

		Public Overrides Function Compare(x As Collections.Generic.KeyValuePair(Of Double, Double), y As Collections.Generic.KeyValuePair(Of Double, Double)) As Integer
			If x.Key < y.Key Then
				Return -1
			ElseIf x.Key > y.Key Then
				Return 1
			Else
				Return 0
			End If
		End Function
	End Class

End Module
