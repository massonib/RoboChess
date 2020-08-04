'!TITLE "<Title>"
PROGRAM Pen2

'**********************PROGRAM INFORMATION*********************
' Author(s):         Joshua Vanrenterghem
' Revision:          1
' Rev Date:          07:1:2020
' Notes:             Uses robot to write with a pen
' Contact Info:      joshua.vanrenterghem@ktmresearch.com
'**********************PROGRAM INFORMATION*********************


'Get the arm and estbalish variables
takearm

defstr line = "0"
defstr circle = "1"
defstr square = "2"
defstr a = "3"
defstr bee = "4"
defstr cee = "5"
defstr firstPen = "9"
defstr secondPen = "8"

defsng offsetX = 0
defsng offsetY = 0
defsng offsetZ = 0
defsng offsetRX = 0
defsng offsetRY = 0
defsng offsetRZ = 0
defint offset = 0
defint pickIndex = 0
defint Li1 = 1
defint li2 = 2
defpos Lp1 = (0, 40, 115, 0, 0, 90)
defpos Lp2 = (0, -40, 115, 0, 0, 90)
Tool 1, Lp1
Tool 2, Lp2

defstr current_Tool

defint index
DIM Fvalue$(25)
defpos currPickPt
defint intCamCom
defint intConState
DEFSTR strFvalue
DEFSTR TEXT
defint firstPos

'******Establish Comms With YAT******
intCamCom = 9
intConState=-1

COM_STATE #intCamCom, intConState
IF intConState = -1 THEN
		COM_ENCOM #intCamCom
		FLUSH #intCamCom
END IF
'******Establish Comms With YAT******


PRINT #intCamCom TEXT

PRINT #intCamCom "Please select the following movements: line - 0, cirlce - 1, square - 2, a - 3, bee - 4, cee - 5"
'***RESETS POSITION
PRINT #intCamCom "Resetting Position..."
MOVE L,(POSX(P[0]), POSY(P[0]), POSZ(P[0]), POSRX(P[0]), POSRY(P[0]), POSRZ(P[0]))
Delay 500

'Recieve data from Cognex camera, put into a string
LINEINPUT #intCamCom strFvalue
Delay 500
PRINT #intCamCom StrFvalue

'Here, we use the set number in the array "Fvalue" to tell the robot to write the desired shape.

IF strFvalue = line THEN
	CHANGETOOL Li1
	PRINT #intCamCom "Writing Line"
	offsetRX = 15
	currPickPt = (POSX(P[2]) + offsetX, POSY(P[2]) + offsetY, POSZ(P[2]) + offsetZ, POSRX(P[2]) + offsetRX, POSRY(P[2]) + offsetRY, POSRZ(P[2]) + offsetRZ)
	move l, currPickPt
	offsetY = 100
	currPickPt = (POSX(P[2]) + offsetX, POSY(P[2]) + offsetY, POSZ(P[2]) + offsetZ, POSRX(P[2]) + offsetRX, POSRY(P[2]) + offsetRY, POSRZ(P[2]) + offsetRZ)
	move l, currPickPt
	offsetX = -100
	currPickPt = (POSX(P[2]) + offsetX, POSY(P[2]) + offsetY, POSZ(P[2]) + offsetZ, POSRX(P[2]) + offsetRX, POSRY(P[2]) + offsetRY, POSRZ(P[2]) + offsetRZ)
	move l, currPickPt
	offsetY = -100
	currPickPt = (POSX(P[2]) + offsetX, POSY(P[2]) + offsetY, POSZ(P[2]) + offsetZ, POSRX(P[2]) + offsetRX, POSRY(P[2]) + offsetRY, POSRZ(P[2]) + offsetRZ)
	move l, currPickPt
	offsetX = 100
	currPickPt = (POSX(P[2]) + offsetX, POSY(P[2]) + offsetY, POSZ(P[2]) + offsetZ, POSRX(P[2]) + offsetRX, POSRY(P[2]) + offsetRY, POSRZ(P[2]) + offsetRZ)
	move l, currPickPt


	PRINT #intCamCom "Wrote Line"
END IF

IF strFvalue = circle THEN
	PRINT #intCamCom "Writing Circle"
	MOVE L,(POSX(P[3]), POSY(P[3]), POSZ(P[3]), POSRX(P[3]), POSRY(P[3]), POSRZ(P[3]))
END IF

IF strFvalue = square THEN
	PRINT #intCamCom "Writing Square"
	Approach l, P5, 0
	move l, P6
	move l, P5
	move l, P6
	move l, P8
END IF

IF strFvalue = a THEN
	PRINT #intCamCom "Writing Circle"
	MOVE L,(POSX(P[3]), POSY(P[3]), POSZ(P[3]), POSRX(P[3]), POSRY(P[3]), POSRZ(P[3]))
END IF

IF strFvalue = bee THEN
	PRINT #intCamCom "Writing Circle"
	MOVE L,(POSX(P[3]), POSY(P[3]), POSZ(P[3]), POSRX(P[3]), POSRY(P[3]), POSRZ(P[3]))
END IF

IF strFvalue = cee THEN
	PRINT #intCamCom "Writing Circle"
	MOVE L,(POSX(P[3]), POSY(P[3]), POSZ(P[3]), POSRX(P[3]), POSRY(P[3]), POSRZ(P[3]))
END IF

END




   
