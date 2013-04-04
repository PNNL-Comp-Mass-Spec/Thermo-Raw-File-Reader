﻿Option Strict On

Imports ThermoRawFileReaderDLL.FinniganFileIO

Module modMain

	Public Sub Main()

		If False Then
			Dim strRawFilePath As String = "..\60999_MouseIslet_HOM_LC_4Oct12_Samwise_12-05-13.raw"
			Dim intPointsPerSpectrumToKeep As Integer = 250

			ConvertRawFileToCSVForLars(strRawFilePath, intPointsPerSpectrumToKeep)

		Else
			'TestReader("..\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")
			'TestReader("..\QC_Shew_12_02-250ng-Multiplex_08Jan13_Frodo_12-2-34.raw")

			' Uncomment the following to test the GetCollisionEnergy() function
			'TestReader("..\EDRN_ERG_Spop_ETV1_50fmolHeavy_0p5ugB53A_Frac48_3Oct12_Gandalf_W33A1_16a.raw")

			TestReader("..\CPTAC_10Pep_UndepPlas__Schedule_24Mar13_Gandalf_W22511A1.raw")
		End If

		Console.WriteLine("Done")

	End Sub

	Private Sub ConvertRawFileToCSVForLars(ByVal strRawFilePath As String, ByVal intPointsPerSpectrumToKeep As Integer)

		Try
			Dim fiOutputFile As System.IO.FileInfo
			Dim strOutputFilePath As String
			strOutputFilePath = IO.Path.ChangeExtension(strRawFilePath, ".csv")
			fiOutputFile = New System.IO.FileInfo(strOutputFilePath)

			If intPointsPerSpectrumToKeep > 0 Then
				strOutputFilePath = IO.Path.Combine(fiOutputFile.Directory.FullName, IO.Path.GetFileNameWithoutExtension(fiOutputFile.Name) & "_Top" & intPointsPerSpectrumToKeep & IO.Path.GetExtension(fiOutputFile.Name))
			End If

			Dim oReader As XRawFileIO
			oReader = New XRawFileIO()

			oReader.OpenRawFile(strRawFilePath)

			Dim iNumScans = oReader.GetNumScans()

			Dim udtScanHeaderInfo As FinniganFileReaderBaseClass.udtScanHeaderInfoType
			Dim bSuccess As Boolean
			Dim intDataCount As Integer
			Dim intDataCountToWrite As Integer

			Dim intStartIndex As Integer
			Dim dblMzList() As Double
			Dim dblIntensityList() As Double

			ReDim dblMzList(0)
			ReDim dblIntensityList(0)

			udtScanHeaderInfo = New FinniganFileReaderBaseClass.udtScanHeaderInfoType

			Using swOutFile As System.IO.StreamWriter = New System.IO.StreamWriter(New System.IO.FileStream(strOutputFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

				For iScanNum As Integer = 1 To iNumScans

					bSuccess = oReader.GetScanInfo(iScanNum, udtScanHeaderInfo)
					If bSuccess Then

						If iScanNum Mod 500 = 0 Then
							Console.WriteLine("Scan " & iScanNum & " at " & udtScanHeaderInfo.RetentionTime.ToString("0.00") & " minutes")
						End If

						intDataCount = oReader.GetScanData(iScanNum, dblMzList, dblIntensityList, udtScanHeaderInfo)

						swOutFile.WriteLine("SPECTRUM - MS,")
						swOutFile.WriteLine(IO.Path.GetFileName(strRawFilePath) & ",")
						swOutFile.WriteLine(udtScanHeaderInfo.FilterText & ",")

						If intPointsPerSpectrumToKeep = 0 OrElse intDataCount <= intPointsPerSpectrumToKeep Then
							intDataCountToWrite = intDataCount
						Else
							intDataCountToWrite = intPointsPerSpectrumToKeep
						End If

						swOutFile.WriteLine("Scan #: " & iScanNum.ToString() & ",")
						swOutFile.WriteLine("RT: " & udtScanHeaderInfo.RetentionTime.ToString("0.00") & ",")
						swOutFile.WriteLine("Data points: " & intDataCountToWrite.ToString() & ",")
						swOutFile.WriteLine("Mass,Intensity")

						If intDataCount <= intDataCountToWrite Then

							For iDataPoint As Integer = 0 To dblMzList.Length - 1
								ConvertRawFileToCSVForLarsWriteValue(swOutFile, dblMzList(iDataPoint), dblIntensityList(iDataPoint))
							Next

						Else

							Dim lstDataToKeep As Generic.List(Of Generic.KeyValuePair(Of Double, Double))
							lstDataToKeep = New Generic.List(Of Generic.KeyValuePair(Of Double, Double))

							' Filter the data by intensity
							Array.Sort(dblIntensityList, dblMzList)

							' Determine the start index of the data to keep
							intStartIndex = intDataCount - intPointsPerSpectrumToKeep
							If intStartIndex < 0 Then intStartIndex = 0

							' Populate lstDataToKeep with the data we want to keep
							For intIndex As Integer = intStartIndex To dblMzList.Length - 1
								lstDataToKeep.Add(New Generic.KeyValuePair(Of Double, Double)(dblMzList(intIndex), dblIntensityList(intIndex)))
							Next

							lstDataToKeep.Sort(New clsMzListComparer)

							For iDataPoint As Integer = 0 To lstDataToKeep.Count - 1
								ConvertRawFileToCSVForLarsWriteValue(swOutFile, lstDataToKeep(iDataPoint).Key, lstDataToKeep(iDataPoint).Value)
							Next

						End If

					End If
				Next

			End Using

			oReader.CloseRawFile()

		Catch ex As Exception
			Console.WriteLine("Error in sub ConvertRawFileToCSVForLars: " & ex.Message)
		End Try

	End Sub

	Private Sub ConvertRawFileToCSVForLarsWriteValue(ByRef swOutFile As System.IO.StreamWriter, ByVal dblMZ As Double, dblIntensity As Double)
		swOutFile.WriteLine(dblMZ.ToString("0.00000") & "," & dblIntensity.ToString("0.00"))
	End Sub

	Private Sub TestReader(strRawFilePath As String)
		Try
			Dim oReader As XRawFileIO
			oReader = New XRawFileIO()

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

			Dim lstCollisionEnergies As System.Collections.Generic.List(Of Double)
			Dim strCollisionEnergies As String = String.Empty

			ReDim dblMzList(0)
			ReDim dblIntensityList(0)

			udtScanHeaderInfo = New FinniganFileReaderBaseClass.udtScanHeaderInfoType

			ShowMethod(oReader)

			For iScanNum As Integer = 1 To iNumScans

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

					If iScanNum Mod 50 = 0 Then
						intDataCount = oReader.GetScanData(iScanNum, dblMzList, dblIntensityList, udtScanHeaderInfo)
						For iDataPoint As Integer = 0 To dblMzList.Length - 1 Step 50
							Console.WriteLine("  " & dblMzList(iDataPoint).ToString("0.000") & " mz   " & dblIntensityList(iDataPoint).ToString("0"))
						Next
						Console.WriteLine()
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

		Public Overrides Function Compare(x As System.Collections.Generic.KeyValuePair(Of Double, Double), y As System.Collections.Generic.KeyValuePair(Of Double, Double)) As Integer
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
