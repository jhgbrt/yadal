for /f %%i in ('dir /b ..\packages\Join.CSharp.*') do set p=%%i
..\packages\%p%\tools\joincs.exe ..\Net.Code.ADONet .\Db.cs