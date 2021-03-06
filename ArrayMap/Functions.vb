﻿Imports ExcelDna.Integration
Imports ExcelDna.Integration.XlCall
Imports Microsoft.VisualBasic.FileIO

Public Module Functions

    <ExcelFunction(Name:="ARRAY.MAP", Description:="Evaluates the function for every value in the input array, returning an array that has the same size as the input.")>
    Function ArrayMap(
                     <ExcelArgument(Description:="The function to evaluate - either enter the name without any quotes or brackets (for .xll functions), or as a string (for VBA functions)")>
                     [function] As Object,
                     <ExcelArgument(Description:="The array of input values (row, column or rectangular range) ")>
                     input As Object)

        Dim evaluate As Func(Of Object, Object)

        If TypeOf [function] Is Double Then
            evaluate = Function(x) Excel(xlUDF, [function], x)
        ElseIf TypeOf [function] Is String Then
            ' First try to get the RegisterId, if it's an .xll UDF
            Dim registerId As Object
            registerId = Excel(xlfEvaluate, [function])
            If TypeOf registerId Is Double Then
                evaluate = Function(x) Excel(xlUDF, registerId, x)
            Else
                ' Just call as string, hoping it's a valid VBA function
                evaluate = Function(x) Excel(xlUDF, [function], x)
            End If
        Else
            Return ExcelError.ExcelErrorValue
        End If

        Return ArrayEvaluate(evaluate, input)
    End Function

    <ExcelFunction(Name:="ARRAY.MAP2", Description:="Evaluates the two-argument function for every value in the first and second inputs. " &
                   "Takes a single value and any rectangle, or one row and one column, or one column and one row.")>
    Function ArrayMap2(
                     <ExcelArgument(Description:="The function to evaluate - either enter the name without any quotes or brackets (for .xll functions), or as a string (for VBA functions)")>
                     [function] As Object,
                     <ExcelArgument(Description:="The input value(s) for the first argument (row, column or rectangular range) ")>
                     input1 As Object,
                     <ExcelArgument(Description:="The input value(s) for the second argument (row, column or rectangular range) ")>
                     input2 As Object)

        Dim evaluate As Func(Of Object, Object, Object)

        If TypeOf [function] Is Double Then
            evaluate = Function(x, y) Excel(xlUDF, [function], x, y)
        ElseIf TypeOf [function] Is String Then
            ' First try to get the RegisterId, if it's an .xll UDF
            Dim registerId As Object
            registerId = Excel(xlfEvaluate, [function])
            If TypeOf registerId Is Double Then
                evaluate = Function(x, y) Excel(xlUDF, registerId, x, y)
            Else
                ' Just call as string, hoping it's a valid VBA function
                evaluate = Function(x, y) Excel(xlUDF, [function], x, y)
            End If
        Else
            Return ExcelError.ExcelErrorValue
        End If

        If Not TypeOf input1 Is Object(,) Then
            Dim evaluate1 = Function(x) evaluate(input1, x)
            Return ArrayEvaluate(evaluate1, input2)
        ElseIf Not TypeOf input2 Is Object(,) Then
            Dim evaluate1 = Function(x) evaluate(x, input2)
            Return ArrayEvaluate(evaluate1, input1)
        End If

        ' Now we know both input1 and input2 are arrays
        ' We assume they are 1D, else error

        Return ArrayEvaluate2(evaluate, input1, input2)

    End Function

    <ExcelFunction(Name:="ARRAY.MAP3", Description:="Evaluates the three-argument function for arrays values. ")>
    Function ArrayMap3(
                     <ExcelArgument(Description:="The function to evaluate - either enter the name without any quotes or brackets (for .xll functions), or as a string (for VBA functions)")>
                     [function] As Object,
                     <ExcelArgument(Description:="The input value(s) for the first argument (row, column or rectangular range) ")>
                     input1 As Object,
                     <ExcelArgument(Description:="The input value(s) for the second argument (row, column or rectangular range) ")>
                     input2 As Object,
                     <ExcelArgument(Description:="The input value(s) for the third argument (row, column or rectangular range) ")>
                     input3 As Object)

        Dim inputArray1 As Object
        Dim inputArray2 As Object

        Dim evaluate As Func(Of Object, Object, Object)

        Dim functionID As Object

        If TypeOf [function] Is Double Then
            functionID = [function]
        ElseIf TypeOf [function] Is String Then
            ' First try to get the RegisterId, if it's an .xll UDF
            Dim registerId As Object
            registerId = Excel(xlfEvaluate, [function])
            If TypeOf registerId Is Double Then
                functionID = registerId
            Else
                ' Just call as string, hoping it's a valid VBA function
                functionID = [function]
            End If
        Else
            Return ExcelError.ExcelErrorValue
        End If

        If Not TypeOf input1 Is Object(,) Then
            evaluate = Function(x, y) Excel(xlUDF, functionID, input1, x, y)
            inputArray1 = input2
            inputArray2 = input3
        ElseIf Not TypeOf input2 Is Object(,) Then
            evaluate = Function(x, y) Excel(xlUDF, functionID, x, input2, y)
            inputArray1 = input1
            inputArray2 = input3
        ElseIf Not TypeOf input3 Is Object(,) Then
            evaluate = Function(x, y) Excel(xlUDF, functionID, x, y, input3)
            inputArray1 = input1
            inputArray2 = input2
        Else
            Return ExcelError.ExcelErrorValue
        End If

        If Not TypeOf inputArray1 Is Object(,) Then
            Dim evaluate1 = Function(x) evaluate(inputArray1, x)
            Return ArrayEvaluate(evaluate1, inputArray2)
        ElseIf Not TypeOf inputArray2 Is Object(,) Then
            Dim evaluate1 = Function(x) evaluate(x, inputArray2)
            Return ArrayEvaluate(evaluate1, inputArray1)
        End If

        ' Now we know both input1 and input2 are arrays
        ' We assume they are 1D, else error

        Return ArrayEvaluate2(evaluate, inputArray1, inputArray2)

    End Function

    Private Function ArrayEvaluate(evaluate As Func(Of Object, Object), input As Object) As Object
        If TypeOf input Is Object(,) Then
            Dim output(input.GetLength(0) - 1, input.GetLength(1) - 1) As Object

            For i As Integer = 0 To input.GetLength(0) - 1
                For j As Integer = 0 To input.GetLength(1) - 1
                    output(i, j) = evaluate(input(i, j))
                Next
            Next
            Return output
        Else
            Return evaluate(input)
        End If
    End Function

    Private Function ArrayEvaluate2(evaluate As Func(Of Object, Object, Object), input1 As Object(,), input2 As Object(,)) As Object
        If input1.GetLength(0) > 1 Then

            ' Lots of rows in input1, we'll take its first column only, and take the columns from input2
            Dim output(input1.GetLength(0) - 1, input2.GetLength(1) - 1) As Object

            For i As Integer = 0 To input1.GetLength(0) - 1
                For j As Integer = 0 To input2.GetLength(1) - 1
                    output(i, j) = evaluate(input1(i, 0), input2(0, j))
                Next
            Next
            Return output
        Else

            ' Single row in input1, we'll take its columns, and take the rows from input1
            Dim output(input2.GetLength(0) - 1, input1.GetLength(1) - 1) As Object

            For i As Integer = 0 To input2.GetLength(0) - 1
                For j As Integer = 0 To input1.GetLength(1) - 1
                    output(i, j) = evaluate(input1(0, j), input2(i, 0))
                Next
            Next
            Return output
        End If
    End Function

    <ExcelFunction(IsHidden:=True)>
    Function Describe1(x)
        Return x.ToString()
    End Function

    <ExcelFunction(IsHidden:=True)>
    Function Describe2(x, y)
        Return x.ToString() & "|" & y.ToString()
    End Function

    <ExcelFunction(Name:="ARRAY.FROMFILE", Description:="Reads the contents of a delimited file")>
    Function ArrayFromFile(<ExcelArgument("Full path to the file to read")> Path As String,
                           <ExcelArgument(Name:="[SkipHeader]", Description:="Skips the first line of the file - default False")> skipHeader As Object,
                           <ExcelArgument(Name:="[Delimiter]", Description:="Sets the delimiter to accept - default ','")> delimiter As Object)

        Dim lines As New List(Of String())

        Using csvParser As New TextFieldParser(Path)


            If TypeOf delimiter Is ExcelMissing Then
                csvParser.SetDelimiters(New String() {","})
            Else
                csvParser.SetDelimiters(New String() {delimiter})   ' TODO: Accept multiple ?
            End If

            csvParser.CommentTokens = New String() {"#"}
            csvParser.HasFieldsEnclosedInQuotes = True

            If Not TypeOf skipHeader Is ExcelMissing AndAlso (skipHeader = True OrElse skipHeader = 1) Then
                csvParser.ReadLine()
            End If

            Do While csvParser.EndOfData = False
                lines.Add(csvParser.ReadFields())
            Loop
        End Using

        If lines.Count = 0 Then
            Return ""
        End If

        Dim result(lines.Count - 1, lines(0).Length - 1) As Object
        For i As Integer = 0 To lines.Count - 1
            For j As Integer = 0 To lines(0).Length - 1
                result(i, j) = lines(i)(j)
            Next j
        Next i
        Return result

    End Function

    <ExcelFunction(Name:="ARRAY.SKIPROWS", Description:="Returns the remainder of an array after skipping the first n rows")>
    Function ArraySkipRows(<ExcelArgument(AllowReference:=True)> array As Object, rowsToSkip As Integer)
        If TypeOf array Is ExcelReference Then
            Dim arrayRef As ExcelReference = array
            Return New ExcelReference(arrayRef.RowFirst + rowsToSkip, arrayRef.RowLast, arrayRef.ColumnFirst, arrayRef.ColumnLast, arrayRef.SheetId)
        ElseIf TypeOf array Is Object(,) Then
            Dim arrayIn As Object(,) = array
            Dim result(array.GetLength(0) - rowsToSkip - 1, array.GetLength(1) - 1) As Object
            For i As Integer = 0 To result.GetLength(0) - rowsToSkip - 1
                For j As Integer = 0 To result.GetLength(1) - 1
                    result(i, j) = arrayIn(i + rowsToSkip, j)
                Next j
            Next i
            Return result
        Else
            Return array
        End If
    End Function

    <ExcelFunction(Name:="ARRAY.COLUMN", Description:="Returns a specified column from an array")>
    Function ArrayColumn(<ExcelArgument(AllowReference:=True)> array As Object, <ExcelArgument("One-based column index to select")> ColIndex As Integer)
        If TypeOf array Is ExcelReference Then
            Dim arrayRef As ExcelReference = array
            Return New ExcelReference(arrayRef.RowFirst, arrayRef.RowLast, arrayRef.ColumnFirst + ColIndex - 1, arrayRef.ColumnFirst + ColIndex - 1, arrayRef.SheetId)
        ElseIf TypeOf array Is Object(,) Then
            Dim arrayIn As Object(,) = array
            Dim result(array.GetLength(0) - 1, 1) As Object
            Dim j As Integer = ColIndex - 1
            For i As Integer = 0 To result.GetLength(0) - 1
                result(i, 0) = arrayIn(i, j)
            Next i
            Return result
        Else
            Return array
        End If
    End Function

End Module
