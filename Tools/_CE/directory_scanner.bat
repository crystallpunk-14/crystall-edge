@echo off
set "output=folders_list.txt"
echo Listing folders recursively... > "%output%"
for /r /d %%i in (*) do echo %%i >> "%output%"
echo Done. Result saved to %output%
pause
