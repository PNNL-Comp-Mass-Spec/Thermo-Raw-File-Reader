@echo off
echo This probably needs to be run from an elevated (administrative level) command prompt
pause

@echo on
C:\Users\d3l243\AppData\Local\Apps\OpenCover\OpenCover.Console.exe -target:"C:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" -targetargs:"""Debug\RawFileReaderTests.dll""" -register:user
C:\Users\d3l243\AppData\Local\Apps\ReportGenerator\ReportGenerator.exe -reports:results.xml -targetdir:coverage

pause
