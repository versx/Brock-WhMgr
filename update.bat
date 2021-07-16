@echo off

SET prjDir=%CD%\WhMgr
SET binDir=%prjDir%\bin\debug\netcoreapp2.1

:: Pull latest Git repository
echo "Pulling latest Git repository changes..."
git pull

:: Build WhMgr.dll
echo "Building WhMgr..."
dotnet build

:: Copy example config
::echo "Copying example files..."
::xcopy /s /e %prjDir%\examples\alerts\* %binDir%\alerts\
::xcopy /s /e %prjDir%\examples\filters\* %binDir%\filters\

echo "Copying latest master file..."
xcopy /s /e %prjDir%\static\data\masterfile.json %binDir%\static\data\masterfile.json
xcopy /s /e %prjDir%\static\data\cpMultipliers.json %binDir%\static\data\cpMultipliers.json

echo "Update Complete"
