Option Strict On

Module modMain

	Public Sub Main()

		Try
			Dim oReader As ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO
			oReader = New ThermoRawFileReaderDLL.FinniganFileIO.XRawFileIO()

			oReader.OpenRawFile("..\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")

			' Uncommen the following to test the GetCollisionEnergy() function
			'oReader.OpenRawFile("..\EDRN_ERG_Spop_ETV1_50fmolHeavy_0p5ugB53A_Frac48_3Oct12_Gandalf_W33A1_16a.raw")

			For intIndex As Integer = 0 To oReader.FileInfo.InstMethods.Length - 1
				Console.WriteLine(oReader.FileInfo.InstMethods(intIndex))
			Next

			Dim iNumScans = oReader.GetNumScans()

			Dim udtScanHeaderInfo As ThermoRawFileReaderDLL.FinniganFileIO.FinniganFileReaderBaseClass.udtScanHeaderInfoType
			Dim bSuccess As Boolean
			Dim intDataCount As Integer

			Dim dblMzList() As Double
			Dim dblIntensityList() As Double

			Dim lstCollisionEnergies As System.Collections.Generic.List(Of Double)
			Dim strCollisionEnergies As String = String.Empty

			ReDim dblMzList(0)
			ReDim dblIntensityList(0)

			udtScanHeaderInfo = New ThermoRawFileReaderDLL.FinniganFileIO.FinniganFileReaderBaseClass.udtScanHeaderInfoType

			For iScanNum As Integer = 1 To iNumScans

				bSuccess = oReader.GetScanInfo(iScanNum, udtScanHeaderInfo)
				If bSuccess Then
					Console.Write("Scan " & iScanNum & " at " & udtScanHeaderInfo.RetentionTime.ToString("0.00") & " minutes: " & udtScanHeaderInfo.FilterText)
					lstCollisionEnergies = oReader.GetCollisionEnergy(iScanNum)

					If lstCollisionEnergies.Count = 0 Then
						strCollisionEnergies = String.Empty
					ElseIf lstCollisionEnergies.Count >= 1 Then
						strCollisionEnergies = lstCollisionEnergies.Item(0).ToString("0.0")
					Else
						For intIndex = 1 To lstCollisionEnergies.Count - 1
							strCollisionEnergies &= ", " & lstCollisionEnergies.Item(intIndex).ToString("0.0")
						Next
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
			Console.WriteLine("Error in sub Main: " & ex.Message)
		End Try

		Console.WriteLine("Done")

	End Sub

End Module
