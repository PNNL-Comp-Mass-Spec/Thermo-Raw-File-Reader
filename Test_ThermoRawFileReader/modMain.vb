Option Strict On

Module modMain

	Public Sub Main()

		Try
			Dim oReader As ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO
			oReader = New ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO()

			oReader.OpenRawFile("..\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")

			For intIndex As Integer = 0 To oReader.FileInfo.InstMethods.Length - 1
				Console.WriteLine(oReader.FileInfo.InstMethods(intIndex))
			Next

			Dim iNumScans = oReader.GetNumScans()

			Dim udtScanHeaderInfo As ThermoRawFileReaderDLL.FinniganFileIO.FinniganFileReaderBaseClass.udtScanHeaderInfoType
			Dim bSuccess As Boolean
			Dim intDataCount As Integer

			Dim dblMzList() As Double
			Dim dblIntensityList() As Double

			ReDim dblMzList(0)
			ReDim dblIntensityList(0)

			udtScanHeaderInfo = New ThermoRawFileReaderDLL.FinniganFileIO.FinniganFileReaderBaseClass.udtScanHeaderInfoType

			For iScanNum As Integer = 1 To iNumScans

				bSuccess = oReader.GetScanInfo(iScanNum, udtScanHeaderInfo)
				If bSuccess Then
					Console.WriteLine("Scan " & iScanNum & " at " & udtScanHeaderInfo.RetentionTime.ToString("0.00") & " minutes: " & udtScanHeaderInfo.FilterText)

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
			Console.WriteLine("Error in sub Main: " & ex.Message)
		End Try

		Console.WriteLine("Done")

	End Sub

End Module
