'!TITLE "<Title>"
PROGRAM calibrate_camera

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
' NOTE: This program is used to calibrate camera to robot orgin in Mode 1

' CAUTION: Prior to running this program, verify that the tool tip has been positioned in the center of the camera using the Cognex program.  
' WARNING: Set P[25] in the Denso controller as described in the documentation.  If this step is not done, a tool crash into the vision system is likely.  
' NOTE:  Run this program when the vision system is setup in a machine.  As long as the vision system does not move, this program will not need to be run again.
'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************

DEFINT DELAYTIME =4000 '(Longer for less vibration)
DEFDBL HOR_STEP = 17.5/2
DEFDBL PER_STEP = 10/2
DEFINT IDI = 0
DEFINT IDJ = 0
DEFINT intCamCom = 8   'Camera comm port (from controller menu)
DEFINT intComState = -1
DEFPOS currentpos
DEFSTR TEXT

DEFINT OnYaxis
DEFINT CurrentBlade

DEFINT Pose_5
DEFINT Debugging 
DEFINT Work_Plane
DEFINT Tool_Number

DIM ConfigDATA#(25)


'Take control of the arm
TAKEARM


'***********************************CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************
#Ifdef __VRC__                   ' Don't edit this block
#Else
Call InitialConfiguration(ConfigDATA())
#Endif

  CurrentBlade = INT(ConfigDATA(0))
  OnYaxis = INT(ConfigDATA(1))

  Pose_5 = INT(ConfigDATA(13))

  Debugging = INT(ConfigDATA(14))

  Work_Plane = INT(ConfigDATA(15))
  Tool_Number = INT(ConfigDATA(16))

CHANGEWORK Work_Plane
CHANGETOOL Tool_Number


'***********************************END OF CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************

			'Debugging code, auto-enabled through a variable set above
IF (Debugging = 1) THEN
	PRINTDBG OnYaxis
ELSE
	'No action
ENDIF


GOSUB *CONNECT_TO_DENSO


'Power the motors on

MOTOR ON

EXTSPEED 1
SPEED 100

'Approach a safe distance above the point
APPROACH L, P[25], 100

'Slow the speed for safe entrance


'Move to P25 within the vision system
MOVE L, P[25]
DELAY DELAYTIME

'Step through internal points for camera calibration

FOR IDI = 0 TO 4
	FOR IDJ = 0 TO 4
		IF OnYaxis = 1 THEN                 'ROBOT Y AXIS IS COLINEAR WITH CAMERA Y AXIS
			MOVE P, (POSX(P[25] ), POSY(P[25] ) + (IDI - 2) * HOR_STEP, POSZ(P[25] ) + (IDJ - 2) * PER_STEP, POSRX(P[25]), POSRY(P[25]), POSRZ(P[25]))
			DELAY DELAYTIME
			currentpos = CURPOS
			'Set first bit to "2" to enable determining robot position in relation to camera FOV
			'Cognex only uses 3rd and 4th postions in this mode, so we send robot current position y and z to it.
#Ifdef __VRC__                   ' Don't edit this block
#Else
			TEXT = "1" + "," + STR$(Pose_5) + "," + STR$(POSX(currentpos)) + "," + STR$(POSY(currentpos)) + "," + STR$(POSZ(currentpos))+","+STR$(CurrentBlade)
			PRINT #intCamCom TEXT
#Endif
			DELAY 1000
		
		ElseIF onYaxis = 0  THEN            'ROBOT X AXIS IS COLINEAR WITH CAMERA Y AXIS
	 		MOVE P, (POSX(P[25] ) + (IDI - 2) * HOR_STEP, POSY(P[25] ), POSZ(P[25] ) + (IDJ - 2) * PER_STEP, POSRX(P[25]), POSRY(P[25]), POSRZ(P[25]))
			DELAY DELAYTIME
			currentpos = CURPOS
			'Set first bit to "2" to enable determining robot position in relation to camera FOV
			'Cognex only uses 3rd and 4th postions in this mode,so we send robot current position x and z to it.
#Ifdef __VRC__                   ' Don't edit this block
#Else
			TEXT = "1" + "," + STR$(Pose_5) + "," + STR$(POSY(currentpos)) + "," + STR$(POSX(currentpos)) + "," + STR$(POSZ(currentpos))+","+STR$(CurrentBlade)
			PRINT #intCamCom TEXT
#Endif
			DELAY 1000

		ELSE
#Ifdef __VRC__                   ' Don't edit this block
#Else
			PRINT #intCamCom "6" + "," + "0" + "," + "0" + "," + "0" + "," + "0"+ "," + "0" 'This sends an error code to the Cognex
#Endif
			TEXT = "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0"+ "," + "0" 'This sets the Cognex to a standby state
#Ifdef __VRC__                   ' Don't edit this block
#Else
			PRINT #intCamCom TEXT
#Endif
			APPROACH P, P[25], 200
			DELAY 1000

		END IF
	NEXT
NEXT

#Ifdef __VRC__                   ' Don't edit this block
#Else
PRINT #intCamCom "8" + "," + "0" + "," + "0" + "," + "0" + "," + "0"+ "," + "0" 'This sends a code to the Cognex camera to let it know the cycle completed successfully
#Endif
DELAY 500
'Sending 0 To disable calibration calibration in Cognex program and go into standby mode
#Ifdef __VRC__                   ' Don't edit this block
#Else
PRINT #intCamCom,"0" + "," + "0" + "," + "0" + "," + "0" + "," + "0"+ "," + "0" 
#Endif
DELAY 1000

'Move robot arm up/clear of station
APPROACH P, P[25], 100

GIVEARM


END


''''''''''''''''''''''''''''''
''''''FUNCTION AREA'''''''''''
''''''''''''''''''''''''''''''

*CONNECT_TO_DENSO:
#Ifdef __VRC__                   ' Don't edit this block
#Else
	COM_STATE #intCamCom, intComState
#Endif
	IF intComState = -1 THEN
#Ifdef __VRC__                   ' Don't edit this block
#Else
	  COM_DISCOM #intCamCom
#Endif
	  WAIT 100
#Ifdef __VRC__                   ' Don't edit this block
#Else
	  COM_ENCOM #intCamCom
	  FLUSH #intCamCom
#Endif
	ELSE
#Ifdef __VRC__                   ' Don't edit this block
#Else
	  FLUSH #intCamCom
#Endif
	END IF
RETURN
