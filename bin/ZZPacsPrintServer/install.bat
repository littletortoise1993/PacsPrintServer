%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe   %~dp0ZZPacsPrintServer.exe
sc config ZZPacsPrintServer start= auto 
Net Start  ZZPacsPrintServer 
pause