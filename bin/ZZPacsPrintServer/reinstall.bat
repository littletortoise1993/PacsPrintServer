%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe /u  %~dp0ZZPacsPrintServer.exe
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe   %~dp0ZZPacsPrintServer.exe
sc config ZZPacsPrintServer start= auto 
Net Start  ZZPacsPrintServer
