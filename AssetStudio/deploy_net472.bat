pyinstaller.exe -F pkg.py -n script.exe
set fname=%date:~2,2%%date:~5,2%%date:~8,2%
set fname=%fname: =0%
rem set mydate=%date:/=%
rem set TIMESTAMP=%mydate%
set OUTPUT=pkg-doctor-net472-%fname%
rmdir /S /Q %OUTPUT%
mkdir %OUTPUT%
set SRC=AssetStudioGUI/bin/Release/net472

robocopy %SRC%/ %OUTPUT% *.dll
robocopy %SRC%/ %OUTPUT%%x64% *.dll
robocopy %SRC%/ %OUTPUT% *.py
robocopy %SRC%/ %OUTPUT% *.exe
robocopy %SRC%/ %OUTPUT% *.config
robocopy %SRC%/x64 %OUTPUT%/x64 *.dll
robocopy dist %OUTPUT%/ *