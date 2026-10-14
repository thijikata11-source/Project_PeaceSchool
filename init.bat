@echo off
mode con: cols=100 lines=30
setlocal enabledelayedexpansion
set prj="Project_PeaceSchool"

:setup
set /p username=" Add your GitHub Username: > "
set /p email=" Add your Github Email Address: > "

:main
cls
echo.
echo ===================================================================================
echo.
echo Username : !username!
echo Email Address: !email!
echo.
echo ===================================================================================
echo.
echo Is this your username and email?
set /p usemail=" type 'yes' to confirm, type 'no' to start again > "
if "!usemail!"=="yes" (
	cls
	goto login
	) else if "!usemail!"=="no" (
		cls
		goto setup
		) else (
			cls
			echo Invalid type, choose specific answer...
			pause
			goto main
			)

:login
git config --global user.name "!username!"
git config --global user.email "!email!"
git config --global init.defaultBranch main
echo Login into your github account...
echo.
:: ================================================================================= > Auth LOGIN to GITHUB
gh auth login
cls
echo.
set /p "dir= Paste your working directory: > "
cls
echo.
echo ===================================================================================
echo.
echo Working Directory : !dir!
echo.
echo ===================================================================================
echo.
echo Confirm this is your working directory?, please check...
set /p dirconf=" type 'yes' to confirm, type 'no' to start again > "
if "!dirconf!"=="yes" (
	cls
	goto initwork
	) else if "!dirconf!"=="no" (
		cls
		goto login
		) else (
			cls
			echo Invalid type, choose specific answer...
			pause
			goto login
			)
			
:initwork
cls
echo.
:: ================================================================================ > Adding REPO
gh repo clone thijikata11-source/Project_PeaceSchool
cd !dir!\!prj!
goto work

:work
cls
echo.
echo                             =================================
echo                                      Github Settings
echo                             =================================
echo.
echo                               1. Initial Project
echo                               2. Check File Changes
echo                               3. Add Files
echo                               4. Commit Changes
echo                               5. Push File Changes
echo.
echo                             =================================
echo.
echo                               6. Update project from source
echo                               7. Remove Folder or File
echo                               8. Add Update as Project
echo                               0. Exit
echo.
echo                             =================================
echo.
set /p working="Choose number to proceed: > "
if "!working!"=="1" (
	goto initproj
	) else if "!working!"=="2" (
		goto initstatus
		) else if "!working!"=="3" (
			goto initaddfiles
			) else if "!working!"=="4" (
				goto initcommit
				) else if "!working!"=="5" (
					goto initpush
					) else if "!working!"=="6" (
						goto initpull
						) else if "!working!"=="7" (
							goto initremove
							) else if "!working!"=="8" (
								goto initaddallfiles
								) else if "!working!"=="0" (
									exit
									) else (
										cls
										echo.
										echo error, unspecified number, choose 1-7 only...
										pause
										goto work
										)

:initproj
cls
git init
pause
goto work

:initstatus
cls
git status
pause
goto work

:initaddfiles
cls
set /p addfiles=" Drag and Drop your folder and files: > "
git add "!addfiles!"
pause
goto work

:initcommit
cls
set /p addmsg="Enter the message to commit (e.g. add file to main): > "
git commit -m "!addmsg!"
pause
goto work

:initpush
cls
git push
pause
goto work

:initpull
cls
git pull
:: git fetch origin
:: git reset --hard main
pause
goto work

:initremove
cls
set /p rmdir=" File or folder to remove: > "
git rm -r "!rmdir!"
pause
goto work

:initaddallfiles
cls
echo.
echo Adding...
git add .
echo Adding all changes successfully
pause
goto work

