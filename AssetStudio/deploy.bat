set mydate=%date:/=%
set TIMESTAMP=%mydate: =_%
set OUTPUT=pkg-doctor-%TIMESTAMP%
rmdir /S /Q %OUTPUT%
mkdir %OUTPUT%
set SRC=AssetStudioGUI/bin/Release

robocopy %SRC%/ %OUTPUT% *.dll
robocopy %SRC%/ %OUTPUT% *.py
robocopy %SRC%/ %OUTPUT% *.exe
robocopy %SRC%/ %OUTPUT% *.config
robocopy %SRC%/x64 %OUTPUT%/x64 *.dll