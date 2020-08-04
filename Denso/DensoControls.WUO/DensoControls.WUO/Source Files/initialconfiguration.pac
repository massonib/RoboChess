'!TITLE "<Title>"
PROGRAM InitialConfiguration(ConfigDATA#(25))


'***********************************PROGRAM INFORMATION***************************************************
' Program By:        KTM Research, LLC
' Client:            Nike IHM
' Author(s):         Weifeng Huang, Douglas Van Bossuyt, David Mandrell
' Revision:          18
' Rev Date:          8-29-2017
' Notes:             Initial release to Nike
' Contact Info:      david@ktmresearch.com
'                    www.ktmresearch.com
'***********************************PROGRAM INFORMATION***************************************************


'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************
' NOTE: This program is used to set variables used across the other programs

'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************


DEFINT  CurrentBlade 
DEFINT  OnYaxis 
DEFINT  CameraX 
DEFINT  CameraY 
DEFDBL  CurveBlade_Intial_Theta_Threshold
DEFDBL  DiamondBlade_Intial_Theta_Threshold
DEFDBL  Offset_Tolerance
DEFINT  NumberofCalibrationCycles

DEFINT Do_Theta

DEFINT Pose_1
DEFINT Pose_2
DEFINT Pose_3
DEFINT Pose_4
DEFINT Pose_5

DEFINT  Work_Plane
DEFINT  Tool_Number

DEFINT Debugging


'***********************************CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************
'CHANGE THIS TO IDENTIFY CURRENT BLADE LOADED INTO ROBOT
'FOR NO TIP
'  CurrentBlade = 0
'FOR DIAMOND TIP BLADE
'  CurrentBlade = 1
'FOR CURVED TIP BLADE
'  CurrentBlade = 2
CurrentBlade = 2

'CHANGE THE BELOW VARIABLE TO SET CAMERA ORIENTATION RELATIVE TO THE ROBOT
'FOR CAMERA Y = ROBOT Y:
'  onYaxis = 1
'FOR CAMERA Y = ROBOT X:
'  onYaxis = 0

OnYaxis = 0

'CHANGE THE BELOW VARIABLE TO SET CAMERA ORIENTATION RELATIVE TO THE ROBOT
'FOR CAMERA X AXIS IN POSITIVE DIRECTION OF TRAVEL FOR DENSO
'  CameraX = 1 
'FOR CAMERA X AXIS IN NEGATIVE DIRECTION OF TRAVEL FOR DENSO
'  CameraX = -1

CameraX = 1

'CHANGE THE BELOW VARIABLE TO SET CAMERA ORIENTATION RELATIVE TO THE ROBOT
'FOR CAMERA Y AXIS IN POSITIVE DIRECTION OF TRAVEL FOR DENSO
'  CameraY = 1 
'FOR CAMERA Y AXIS IN NEGATIVE DIRECTION OF TRAVEL FOR DENSO
'  CameraY = -1

CameraY = 1

'***********************************END OF CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************


'***********************************CONFIGURATION SETTINGS FOR CALIBRATION***************************************************

'THRESHOLD FOR FIRST THETA CALIBRATION FOR CURVEBLADE (BETWEEN 2 - 3) -- NOTE: this number does not correspond to theta rotation directly.  It is a measure of the width of the visible blade in pixels.

CurveBlade_Intial_Theta_Threshold = 4

'THRESHOLD FOR FIRST THETA CALIBRATION FOR DIAMONDBLADE (BETWEEN 40 - 45) -- NOTE: this number does not correspond to theta rotation directly.  It is a measure of the width of the visible blade in pixels.

DiamondBlade_Intial_Theta_Threshold = 40

'FINAL X OFFSET TOLERANCE (MM) -- NOTE: this variable controls the accuracy of the results of tool tip calibration.  Denso robots generally achieve 0.50mm positioning accuracy.  However, repeatability is sometimes higher.  
'	If higher accuracy is desired, try reducing this number.  However, this will increase cycle time.  The number of calibration cycles that will be tried before the program continues is set in the next variable below.

Offset_Tolerance = 0.050

'MAXIMUM NUMBER CYCLES BEFOR STOPPING CALIBRATION
NumberofCalibrationCycles = 6


'CHANGE THIS TO ENABLE/DISABLE BLADE THETA ORIENTATION ALGORITHM
'FOR THETA ENABLE
'  Do_Theta = 1
'FOR THETA DISABLE
'  Do_Theta = 0
Do_Theta = 1


'***********************************END OF CONFIGURATION SETTINGS FOR CALIBRATION***************************************************

'***********************************CONFIGURATION SETTINGS FOR ROBOT MOTION***************************************************


'CHANGE THIS BASED ON BLADE TIP POSE ORDER AS SEEN ON ROBOT -- THIS WILL CHANGE IF A NON-SYMETRIC BLADE IS INSTALLED IN A DIFFERENT ORIENTATION
'  ORIENTATION                     VARIABLE NUMBER
'-------------------------------------------------
'  No Blade Pose                         0
'  Wide Blade Left Orientation           1
'  Wide Blade Right Orientation          2
'  Narrow Blade Front                    3
'  Narrow Blade Back                     4
'
'Pose 1 
  Pose_1 = 1
'Pose 2
  Pose_2 = 2
'Pose 3
  Pose_3 = 3
'Pose 4
  Pose_4 = 4
'Pose 5 -- Only used to calibrate the camera to the real world
  Pose_5 = 1

'***********************************END OF CONFIGURATION SETTINGS FOR ROBOT MOTION***************************************************

'***********************************CONFIGURATION SETTINGS FOR TOOL AND WORK PLANE***************************************************


'CHANGE THIS TO BE THE CORRECT WORK PLANE
Work_Plane = 0

'CHANGE THIS TO BE THE CORRECT TOOL
Tool_Number = 1 
'NOTE NOTE NOTE NOTE: Be sure tool offset is approximately correct before running program!!!!



'***********************************END OF CONFIGURATION SETTINGS FOR TOOL AND WORK PLANE***************************************************

'***********************************CONFIGURATION SETTINGS FOR DEBUGGING***************************************************

'ENABLE DEBUGGING -- THIS WILL ENABLE DEBUGGING MODE TO PRODUCE VERBOSE SYSTEM REPORTS
'FOR DEBUGGING
'  Debugging = 1
'FOR NO NO NO DEBUGGING (NORMAL OPERATION)
'  Debugging = 0
Debugging = 0

'***********************************END OF CONFIGURATION SETTINGS FOR DEBUGGING***************************************************



ConfigDATA(0) = CurrentBlade
ConfigDATA(1) = OnYaxis
ConfigDATA(2) = CameraX
ConfigDATA(3) = CameraY

ConfigDATA(4) = CurveBlade_Intial_Theta_Threshold
ConfigDATA(5) = DiamondBlade_Intial_Theta_Threshold 
ConfigDATA(6) = Offset_Tolerance
ConfigDATA(7) = NumberofCalibrationCycles 
ConfigDATA(8) = Do_Theta

ConfigDATA(9) = Pose_1
ConfigDATA(10) = Pose_2
ConfigDATA(11) = Pose_3
ConfigDATA(12) = Pose_4
ConfigDATA(13) = Pose_5

ConfigDATA(14) = Debugging

ConfigDATA(15) = Work_Plane
ConfigDATA(16) = Tool_Number



END
