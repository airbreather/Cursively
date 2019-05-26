@echo off
REM ===========================================================================
REM Pushes the https://airbreather.github.io/Cursively content to the server
REM (run generate-docs.cmd first)
REM ===========================================================================
pushd %~dp0
pushd doc\obj
rd /s /q gh-pages
git clone --branch gh-pages %~dp0 gh-pages
pushd gh-pages
git rm -r .
xcopy /Q /E /R /Y ..\generated-site-content .
git add .
git commit -m "Update docs.  This was performed automatically."
git push origin gh-pages
popd
rd /s /q gh-pages
popd
git push origin gh-pages
popd
