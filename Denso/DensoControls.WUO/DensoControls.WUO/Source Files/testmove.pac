'!TITLE "<Title>"
PROGRAM TestMove

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
' NOTE: This program will demonstrate the new tool calibration

' CAUTION: Only run this program after running update_tool_offset.  Follow work instructions to avoid a tool crash. 
'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************

DEFINT Pose_1
DEFINT Pose_2
DEFINT Pose_3
DEFINT Pose_4

DEFINT intCamCom = 8
DEFINT intComState = -1

DEFINT Debugging
DEFINT Work_Plane
DEFINT Tool_Number
DIM ConfigDATA#(25)
DEFINT CurrentBlade
DEFSTR strPOSTION
'Take control of arm and power on motor
TAKEARM
MOTOR ON

'***********************************CONFIGURATION SETTINGS FOR VISION SYSTEM***************************************************
Call InitialConfiguration(ConfigDATA())

  CurrentBlade = INT(ConfigDATA(0))
  Pose_1 = INT(ConfigDATA(9))
  Pose_2 = INT(ConfigDATA(10))
  Pose_3 = INT(ConfigDATA(11))
  Pose_4 = INT(ConfigDATA(12))


  Debugging = INT(ConfigDATA(14))

  Work_Plane = INT(ConfigDATA(15))
  Tool_Number = INT(ConfigDATA(16))

CHANGEWORK Work_Plane
CHANGETOOL Tool_Number

'''''''''''''''''''''''''Setup of Robot and Camera Communications''''''''''''''''''''''''

COM_STATE #intCamCom, intComState
IF intComState = -1 THEN
		COM_DISCOM #intCamCom
		WAIT 100
		COM_ENCOM #intCamCom
		FLUSH #intCamCom
	ELSE
		FLUSH #intCamCom
END IF

''''''''''''''''''''''''Begin Main Program''''''''''''''''''''''''''''''''''''''''''''


'Set speeds
EXTSPEED 1
SPEED 100

'Approach top of box 
APPROACH L, P[25], 100
DELAY 1000


'Move through four poses in 360 degrees and then unwind back to starting positon
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 0), NEXT
	DELAY 3000
	FLUSH #intCamCom
	strPOSTION = "3"+ ","+STR$(Pose_1)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(2)
	PRINT #intCamCom strPOSTION
	DELAY 500
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 90), NEXT
	DELAY 3000
	FLUSH #intCamCom
	strPOSTION = "3"+ ","+STR$(Pose_3)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(2)
	PRINT #intCamCom strPOSTION
	DELAY 500
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 180), NEXT
	DELAY 3000
	FLUSH #intCamCom
	strPOSTION = "3"+ ","+STR$(Pose_2)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(2)
	PRINT #intCamCom strPOSTION
	DELAY 500
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 270), NEXT
	DELAY 3000
	FLUSH #intCamCom
	strPOSTION = "3"+ ","+STR$(Pose_4)+ ","+STR$(POSX(P[25]))+","+STR$(POSY(P[25]))+","+STR$(POSZ(P[25]))+","+STR$(CurrentBlade)+","+STR$(3)
	PRINT #intCamCom strPOSTION
	DELAY 500
'MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 360), NEXT
'MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 270), NEXT
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 180), NEXT
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 90), NEXT
MOVE L, (POSX(P[25]), POSY(P[25]), POSZ(P[25]), 0, 0, POSRZ(P[25]) + 0), NEXT

DELAY 500

'Withdraw tool tip from vision system

APPROACH L, P[25], 100
DELAY 1000
COM_DISCOM #intCamCom
'Return arm to controller for next program
GIVEARM


END
