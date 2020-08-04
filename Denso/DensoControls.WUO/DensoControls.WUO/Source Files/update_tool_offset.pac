'!TITLE "<Title>"
PROGRAM update_tool_offset



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
' NOTE: This program will update tool offset information in Mode 3

' CAUTION: Prior to running this program, ensure the camera calibration is still valid. If in doubt, follow work instructions and run calibrate_camera first 
'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************



DEFINT intCamCom 
DEFINT intConState
DEFSTR strPOSTION '[WP1.Y,WP1.Z,WP2.Y,WP2.Z,NP3.Y,NP3.Z,NP4.Y,NP4.Z]
DEFSTR strTIPPOSTION
DEFDBL WP1Y 'Wide Blade Position
DEFDBL WP1Z
DEFDBL WP2Y
DEFDBL WP2Z
DEFDBL NP3Y 'Narrow Blade Position
DEFDBL NP3Z
DEFDBL NP4Y
DEFDBL NP4Z
DEFDBL X_OFFSET
DEFDBL Y_OFFSET
DEFDBL Z_OFFSET
DEFDBL TEMPZ_OFFSET
DEFDBL TEMPZ_OFFSET_CHECK
DEFPOS Current_TOOL_OFFSET 
DEFPOS Cam_TOOL_OFFSET 
DEFPOS New_TOOL_OFFSET
DEFDBL Rotation_Offest
DEFINT Current_TOOL_NUM
DEFSTR TEXT
DEFSTR BladeIndexesSTR
DIM aDATA$(25)
DIM BladeIndexes$(25)
DEFINT BladeIndex 
DEFPOS ROTATEORIG
DEFINT OnYaxis
DEFINT CameraX
DEFINT CameraY
DEFINT CurrentBlade
DEFINT Do_Theta
DEFINT Pose_1
DEFINT Pose_2
DEFINT Pose_3
DEFINT Pose_4
DEFINT Pose_5
DEFDBL Currentscore
DEFDBL Periousscore
DEFDBL MinScore
DEFINT icounter
DEFINT kcounter
DEFINT InitialAngleIndex
DEFDBL InitialAngle
DEFINT FineAngleIndex
DEFDBL FinalAngle
DIM Fvalue$(25)
DEFSTR strFvalue
DEFINT LowerBound
DEFINT UpperBound
DEFINT Debugging
DIM ConfigDATA#(25)
DEFDBL CurveBlade_Intial_Theta_Threshold
DEFDBL DiamondBlade_Intial_Theta_Threshold
DEFDBL Intial_Theta_Threshold
DEFDBL Default_Offset_Tolerance
DEFDBL MaximumOffset
DEFINT CalibrationCounter
DEFINT NumberofCalibrationCycles
DEFDBL CurveBlade_Intial_Theta_UpperBound
DEFDBL DiamondBlade_Intial_Theta_UpperBound
DEFDBL MinimumTheta
DEFDBL Offset1
DEFDBL Offset2
DEFINT Work_plane
DEFINT Tool_Number
DEFINT exit_Loop = 0


'Scores and counters for theta orientation section of code
  LowerBound = -5
  Currentscore = 10000
  Periousscore = 0
  MinScore = 100000
  icounter = 0
  kcounter = 0
  UpperBound = LowerBound*2
  InitialAngleIndex = 100000

'counter for rounds of calibration
  CalibrationCounter = 0

'Take control of arm
TAKEARM

'***********************************LOADING CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************


Call InitialConfiguration(ConfigDATA())

  CurrentBlade = ConfigDATA(0)
  OnYaxis = ConfigDATA(1)
  CameraX = INT(ConfigDATA(2))
  CameraY = INT(ConfigDATA(3))
  CurveBlade_Intial_Theta_Threshold = ConfigDATA(4)
  DiamondBlade_Intial_Theta_Threshold = ConfigDATA(5)
  Default_Offset_Tolerance = ConfigDATA(6)
  NumberofCalibrationCycles = INT(ConfigDATA(7))

  Do_Theta = INT(ConfigDATA(8))

  Pose_1 = INT(ConfigDATA(9))
  Pose_2 = INT(ConfigDATA(10))
  Pose_3 = INT(ConfigDATA(11))
  Pose_4 = INT(ConfigDATA(12))
  Pose_5 = INT(ConfigDATA(13))

  Debugging = INT(ConfigDATA(14))

  Work_Plane = INT(ConfigDATA(15))
  Tool_Number = INT(ConfigDATA(16))

CHANGEWORK Work_Plane
CHANGETOOL Tool_Number


'***********************************END OF CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************



'''''''''''''''''''''''''Setup of Robot and Camera Communications''''''''''''''''''''''''
intCamCom = 8
intConState=-1
'Open connection to camera

COM_STATE #intCamCom, intConState
IF intConState = -1 THEN
		COM_DISCOM #intCamCom
		WAIT 100
		COM_ENCOM #intCamCom
		FLUSH #intCamCom
	ELSE
		FLUSH #intCamCom
END IF

'''''''''''''''''''''''''Begin Main Program''''''''''''''''''''''''''''''''''''''''''''


'Initialize tool offset calibration mode in Cognex camera
PRINT #intCamCom "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0"

'Power on motors
MOTOR ON

EXTSPEED 1
SPEED 100

'Move to a safe distance above imaging area
APPROACH L, P[25], 100
DELAY 1000

'Move down to imaging area

MOVE L, P[25]
DELAY 2000

'''''''
'SECTION 1 -- BLADE THETA ORIENTATION
''''''''

'Conditionally enable/disable theta rotation using the Do_Theta variable set above.
IF (Do_Theta = 1) THEN

'Check which Threshold should be used
	IF CurrentBlade = 1 THEN
		Intial_Theta_Threshold = DiamondBlade_Intial_Theta_Threshold
		MinimumTheta = 39
		ELSEIF CurrentBlade = 2 THEN
			Intial_Theta_Threshold = CurveBlade_Intial_Theta_Threshold
			MinimumTheta = 0.6
		ELSE	
			PRINTDBG "Undefined Current Blade Type"
			COM_DISCOM #intCamCom
			GIVEARM
			END

	ENDIF


	MOVE L, P[25]

    'Use coarse movements to determine general blade theta orientation
	WHILE (icounter < 10)	
		MOVE L,(POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]), POSRZ(P[25])+90 +LowerBound+icounter)
		Delay 500
		'Send data to Cognex camera
		TEXT = "2" + "," + "3" + "," + STR$(POSX( P[25])) + "," + STR$(POSY( P[25])) + "," + STR$(POSZ( P[25]))+","+STR$(CurrentBlade)
		FLUSH #intCamCom
		PRINT #intCamCom TEXT
		Delay 500
		'Recieve data from Cognex camera, put into a string
		LINEINPUT #intCamCom, strFvalue
		Delay 500
		'Process string to output data from Cognex camera into a useable variable type -- uses token.pac program to perform operation
		CALL token(strFvalue, ",", Fvalue())
		Currentscore = VAL(Fvalue(0))
		'If the new score from the Cognex camera is better than the current score, save the value and the theta angle at which the value was taken
		IF(Currentscore < MinScore) THEN
			MinScore = Currentscore
			InitialAngleIndex = icounter

		ELSE

		ENDIF

		icounter = icounter+1	
	WEND

	InitialAngle = InitialAngleIndex+LowerBound

	'Debugging code, auto-enabled through a variable set above
	IF (Debugging = 1) THEN
		PRINTDBG STR$(InitialAngleIndex)
		PRINTDBG STR$(MinScore)
		PRINTDBG STR$(InitialAngle)
	ELSE
		'No action
	ENDIF
	'If initial MinScore larger than Threshold, it fails 
	IF MinScore > Intial_Theta_Threshold

		PRINTDBG "Fail to find initial theta"  
		COM_DISCOM #intCamCom
		GIVEARM
		END
	ENDIF

	'Fine tune 

	MOVE L,(POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]), POSRZ(P[25])+90+InitialAngle)
	DELAY 1000
	
    'reset scores and counters
	Currentscore = 10000
	MinScore = 10000
	icounter = 0
	InitialAngleIndex = 100000

    'Use fine movements to determine fine blade theta orientation
	WHILE (icounter < 20)		
		MOVE L,(POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]), POSRZ(P[25])+90+InitialAngle-1+0.1*icounter)
		Delay 500
		'Send data to Cognex camera
		TEXT = "2" + "," + "3" + "," + STR$(POSX( P[25])) + "," + STR$(POSY( P[25])) + "," + STR$(POSZ( P[25]))+","+STR$(CurrentBlade)
		FLUSH #intCamCom
		PRINT #intCamCom TEXT
		Delay 500
		'Receive data from Cognex camera, put into a string
		LINEINPUT #intCamCom, strFvalue
		Delay 500
		'Process string to output data from Cognex camera into a useable variable type -- uses token.pac program to perform operation
		CALL token(strFvalue, ",", Fvalue())
		Currentscore= VAL(Fvalue(0))
		'If the new score from the Cognex camera is better than the current score, save the value and the theta angle at which the value was taken
		IF(Currentscore < MinScore)
			MinScore = Currentscore
			FineAngleIndex = icounter
		ENDIF
		icounter = icounter+1	
	WEND

	'Calculation of final tool angle
	FinalAngle = InitialAngle-1+0.1*FineAngleIndex

	'Debugging code, auto-enabled through a variable set above
	IF (Debugging = 1) THEN
		PRINTDBG STR$(MinScore)
		PRINTDBG STR$(FinalAngle)
	ELSE
		'No action
	ENDIF


	'Update current tool offset with new theta orientation information
	Current_TOOL_OFFSET = TOOLPOS(CURTOOL)
	New_TOOL_OFFSET = ( POSX(Current_TOOL_OFFSET), POSY(Current_TOOL_OFFSET), POSZ(Current_TOOL_OFFSET),POSRX(Current_TOOL_OFFSET), POSRY(Current_TOOL_OFFSET), POSRZ(Current_TOOL_OFFSET)-FinalAngle)

	'Save the updated tool offset
	TOOL 1, New_TOOL_OFFSET
	CHANGETOOL 1

ELSE

	'no action

ENDIF

'Check theta

MOVE L,(POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]), POSRZ(P[25])+90)
TEXT = "2" + "," + "3" + "," + STR$(POSX( P[25])) + "," + STR$(POSY( P[25])) + "," + STR$(POSZ( P[25]))+","+STR$(CurrentBlade)
		FLUSH #intCamCom
		PRINT #intCamCom TEXT
delay 3000


''''''''
''END OF SECTION 1
'''''''''


''''''''
'' SECTION 2 ---- Calibrate X,Y,Z tool tip position until satisfy defult offset torlerance
'''''''''



DO  
	FLUSH #intCamCom 
	'Pose 1
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) )
	DELAY 2000
	'send data of current position and action to take to Cognex
	strPOSTION = "3"+ ","+STR$(Pose_1)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(1)
	PRINT #intCamCom strPOSTION
	DELAY 2000
	FLUSH #intCamCom 
	DELAY 500


	'Pose 2
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25])+ 90)
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25])+ 180) ' Note the extra move command is to force the Denso to rotate a specific direction
	DELAY 2000
	'send data of current position and action to take to Cognex
	strPOSTION = "3"+ ","+STR$(Pose_2)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(1)
	PRINT #intCamCom strPOSTION
	DELAY 2000
	FLUSH #intCamCom 
	DELAY 500

	'Pose 3
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 90)
	DELAY 2000
	'send data of current position and action to take to Cognex
	strPOSTION = "3"+ ","+STR$(Pose_3)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(1)
	PRINT #intCamCom strPOSTION
	DELAY 2000
	FLUSH #intCamCom 
	DELAY 500

	'Pose 4
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 180)
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 270) ' Note the extra move command is to force the Denso to rotate a specific direction
	DELAY 2000
	'send data of current position and action to take to Cognex
	strPOSTION = "3"+ ","+STR$(Pose_4)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(1)

	FLUSH #intCamCom 
	DELAY 2000
	PRINT #intCamCom strPOSTION
	DELAY 1000


	'Read offset information from the camera in a string
	LINEINPUT #intCamCom, strTIPPOSTION

	'Unwind the Denso to prevent hitting soft limit in J6
	DELAY 2000
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 180)
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 90)
	MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), POSRX(P[25]), POSRY(P[25]),  POSRZ(P[25]) + 0)

	'Call program "token" to unpack the string received from the Cognex camera and populate it into an array
	CALL token(strTIPPOSTION, ",", aDATA())


	'Assign Position Values -- from camera's perspective for axes -- unpacked from the array created above into more easily accessible double variables
	WP1Y = VAL(aDATA(0)) ' wide blade position - Y axis
	WP1Z = VAL(aDATA(1)) ' wide blade position - Z axis
	WP2Y = VAL(aDATA(2)) ' wide blade position - Y axis
	WP2Z = VAL(aDATA(3)) ' wide blade position - Z axis
	NP3Y = VAL(aDATA(4)) ' narrow blade position - Y axis
	NP3Z = VAL(aDATA(5)) ' narrow blade position - Z axis
	NP4Y = VAL(aDATA(6)) ' narrow blade position - Y axis
	NP4Z = VAL(aDATA(7)) ' narrow blade positon - Z axis



	IF Debugging = 1
		PRINTDBG "Wide point and narrow point data:",STR$(WP1Y),STR$(WP2Y),STR$(NP3Y),STR$(NP4Y)
		PRINTDBG aDATA(0) ' wide blade position - Y axis
		PRINTDBG aDATA(1) ' wide blade position - Z axis
		PRINTDBG aDATA(2) ' wide blade position - Y axis
		PRINTDBG aDATA(3) ' wide blade position - Z axis
		PRINTDBG aDATA(4) ' narrow blade position - Y axis
		PRINTDBG aDATA(5) ' narrow blade position - Z axis
		PRINTDBG aDATA(6) ' narrow blade position - Y axis
		PRINTDBG aDATA(7) ' narrow blade positon - Z axis
	ELSE
		'No action

	ENDIF

	'Decide Postive or Negative X, Y, and Z Offset based on the order of the blade tips shown in the Cognex Y axis

	Y_OFFSET = (WP1Y - WP2Y)/2

	X_OFFSET = (NP4Y - NP3Y)/2

	TEMPZ_OFFSET = (WP1Z - WP2Z)/2

	TEMPZ_OFFSET_CHECK = (NP3Z - NP4Z)/2

	Z_OFFSET = (TEMPZ_OFFSET+TEMPZ_OFFSET_CHECK)/2


				'Debugging code, auto-enabled through a variable set above
	IF (Debugging = 1) THEN
		PRINTDBG "X Offset, Y Offset, Z Offset:",STR$(X_OFFSET),STR$(Y_OFFSET),STR$(Z_OFFSET)
	ELSE
		'No action
	ENDIF


	'Set the tool offset based on the orientation of the camera

	Current_TOOL_OFFSET = TOOLPOS(CURTOOL)

	IF OnYAxis = 1 THEN

		'Nothing to change.  Camera and robot are aligned on the same Y axis

		New_TOOL_OFFSET = ( POSX(Current_TOOL_OFFSET)+(X_OFFSET*CameraX), POSY(Current_TOOL_OFFSET)+(Y_OFFSET*CameraY), POSZ(Current_TOOL_OFFSET)+(Z_OFFSET),POSRX(Current_TOOL_OFFSET), POSRY(Current_TOOL_OFFSET), POSRZ(Current_TOOL_OFFSET))
	
		' Save new tool to TOOL 1

		DELAY 1000

		PRINTDBG "XOFFEST"
		PRINTDBG STR$(X_OFFSET)
		PRINTDBG "YOFFEST"
		PRINTDBG STR$(Y_OFFSET)

		TOOL 1, New_TOOL_OFFSET 

	strPOSTION = "3"+ ","+STR$(Pose_4)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)

	ELSEIF OnYAxis = 0 THEN

		'Change axis orientations in the below expression

		New_TOOL_OFFSET = ( POSX(Current_TOOL_OFFSET)+(Y_OFFSET*CameraY), POSY(Current_TOOL_OFFSET)+(X_OFFSET*CameraX), POSZ(Current_TOOL_OFFSET)+(Z_OFFSET),POSRX(Current_TOOL_OFFSET), POSRY(Current_TOOL_OFFSET), POSRZ(Current_TOOL_OFFSET))
	
		DELAY 1000

		PRINTDBG "XOFFEST"
		PRINTDBG STR$(X_OFFSET)
		PRINTDBG "YOFFEST"
		PRINTDBG STR$(Y_OFFSET)
		' Save new tool to TOOL 1

		TOOL 1, New_TOOL_OFFSET

	ELSE
		PRINT #intCamCom "6" + "," + "0" + "," + "0" + "," + "0" + "," + "0" 'This sends an error code to the Cognex
		DELAY 1000
		COM_DISCOM #intCamCom
		GIVEARM
		END ' don't set the tool tip offset because something went wrong

	END IF

					'Debugging code, auto-enabled through a variable set above
		IF (Debugging = 1) THEN
			PRINTDBG "X Offset*CameraX, Y Offset*CameraY, Z Offset:",STR$(X_OFFSET*CameraX),STR$(Y_OFFSET*CameraY),STR$(Z_OFFSET)
		ELSE
			'No action
		ENDIF
	''''
	CHANGETOOL 1


	''''Below code checks the tool tip offset and decides to exit the offset routine or not

		Offset1 = ABS( WP1Y - WP2Y)/2
		Offset2 = ABS( NP3Y - NP4Y)/2

		IF Offset1>Offset2 THEN
			MaximumOffset = Offset1 
			ELSE
			MaximumOffset = Offset2
		ENDIF

	
		IF Debugging = 1

			PRINTDBG "MaximumOffset FROM OTHER PROGRAM"
			PRINTDBG STR(MaximumOffset)
		ENDIF
	
		PRINTDBG "Current Offest"
		PRINTDBG STR(MaximumOffset)
		PRINTDBG "Count"
		PRINTDBG STR(CalibrationCounter)
		CalibrationCounter = CalibrationCounter +1

	IF (MaximumOffset<Default_Offset_Tolerance) THEN
		exit_Loop = 1
		PRINTDBG "Maximum Offset reached to exit loop"
    ELSEIF (NumberofCalibrationCycles = CalibrationCounter) THEN
		exit_Loop = 1
		PRINTDBG "Number of calibration cycles reached to exit loop"
    ELSE
	    PRINTDBG "keep looping"
	ENDIF


LOOP UNTIL (exit_Loop = 1)

APPROACH L, P[25], 100

DELAY 1000

'Close communications with camera, give arm back to controller, end program
PRINT #intCamCom "7" + "," + "0" + "," + "0" + "," + "0" + "," + "0" 'This sends a code to the Cognex to let it know the cycle completed successfully
DELAY 500
PRINT #intCamCom "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0" 'This sends a code to the Cognex to put it into standby mode
COM_DISCOM #intCamCom
GIVEARM

''''''''
''END OF SECTION 3
'''''''''


END

