pyinstaller.exe -F pkg.py -n script.exe

set mydate=%date:/=%
set TIMESTAMP=%mydate%
set OUTPUT=pkg-doctor-net472-%TIMESTAMP%
rmdir /S /Q %OUTPUT%
mkdir %OUTPUT%
set SRC=AssetStudioGUI/bin/Release/net472

robocopy %SRC%/ %OUTPUT% *.dll
robocopy %SRC%/ %OUTPUT% *.py
robocopy %SRC%/ %OUTPUT% *.exe
robocopy %SRC%/ %OUTPUT% *.config
robocopy %SRC%/x64 %OUTPUT%/x64 *.dll
robocopy dist %OUTPUT%/ *