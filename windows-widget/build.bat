@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find .NET Framework C# compiler at %CSC%
  exit /b 1
)
"%CSC%" /nologo /optimize+ /target:winexe /out:BouquetWidget.exe /r:System.Drawing.dll /r:System.Windows.Forms.dll BouquetWidget.cs
if errorlevel 1 exit /b 1
echo Built BouquetWidget.exe successfully!
