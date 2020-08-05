'!TITLE "<Title>"
PROGRAM RoboChess

'Set the robot to manual and move it to the center of the bottom left square of the board.
'Copy the point X,Y,Z values to set the origin
defDbl xOrigin = 277.17
defDbl yOrigin = 207.41
defDbl pickupHeight = 475
defdbl RX = -180
defdbl RY = 0
defdbl RZ = 175 'Rotated diagonal, so as to provide the most clearance possible
defDbl slowBelowZ = pickupHeight + 20

'Variables
defInt intCamCom = 8
defInt intConState =-1
DEFSTR strFvalue
dim Fvalue$(25)
defInt actionID
defDbl gotoX
defDbl gotoY
defDbl gotoZ
defInt terminatedSuccessfully
'The rank and file IDs. File: a = 0, b = 1 ... Rank: 1 = 0, 2 = 1, ...
defInt xOffset
defInt yOffset
defDbl moveHeight = pickupHeight + 120
defDbl increment = 55.5 'Change if using a different board

'Set the robot home. This should not change.
defdbl homeX = 100
defdbl homeY = 0
defdbl homeZ = 650

defdbl takenUnitsX = xOrigin
defdbl takenUnitsY = yOrigin - 500
defdbl extraQueenX = xOrigin
defdbl extraQueenY = yOrigin + 100
defint pickup = -1
defint speedVal
defint num = 0

'Setup Robot and Camera Communications
TakeArm 

'''''''''''''''''''''''''Setup of Robot and Camera Communications''''''''''''''''''''''''
COM_STATE #intCamCom, intConState
IF intConState = -1 THEN
		COM_DISCOM #intCamCom
		WAIT 100
		COM_ENCOM #intCamCom
		FLUSH #intCamCom
	ELSE
		FLUSH #intCamCom
END IF

'Power the motors on
MOTOR ON
SPEED 100

DEFINT counter = 0
While counter = 0

	LINEINPUT #intCamCom, strFvalue, WTIME=100,RVAL=terminatedSuccessfully
	FLUSH #intCamCom

	if(terminatedSuccessfully = 1) then
		
		CALL token(strFvalue, ",", Fvalue())
		actionID = val(Fvalue(0))
		xOffset = val(Fvalue(1))
		yOffset = val(Fvalue(2))
		num = val(Fvalue(3))
		FLUSH #intCamCom
		PRINT #intCamCom "(R,Recieved: " + strFvalue + ")"

		if(actionID = -1) then
			'Not a valid command.
		else 
			'There are six action IDs.
			'0. Initialize/reset the robot to home
			'1. Move the arm to given rank/file
			'2. Lower the arm
			'3. Raiser the arm
			'4. Move the arm to the drop-off location
			'5. Move the arm to the queen on the side of the board
			if(actionID = 0) then			
				gotoX = homeX
				gotoY = homeY
				MOVE L,(gotoX, gotoY , homeZ, RX, RY, RZ), SPEED = 25, ACCEL = 1, DECEL = 1
			elseif (actionID = 1) then				
				gotoX = xOrigin + xOffset * increment
				gotoY = yOrigin - yOffset * increment 
				MOVE L,(gotoX, gotoY , moveHeight, RX, RY, RZ), SPEED = 25, ACCEL = 1, DECEL = 1
			elseif (actionID = 2) then
				MOVE L,(gotoX, gotoY , pickupHeight, RX, RY, RZ), SPEED = 5, ACCEL = 1, DECEL = 1
			elseif (actionID = 3) then
				MOVE L,(gotoX, gotoY , moveHeight, RX, RY, RZ), SPEED = 25, ACCEL = 1, DECEL = 1
			elseif (actionID = 4) then
				'When actionID 4 is sent, it also send the rank and file offsets for the taken pieces
				gotoX = takenUnitsX + xOffset * increment 
				gotoY = takenUnitsY - yOffset * increment
				MOVE L,(gotoX, gotoY , moveHeight, RX, RY, RZ), SPEED = 25, ACCEL = 1, DECEL = 1
			elseif (actionID = 5) then
				gotoX = extraQueenX 
				gotoY = extraQueenY
				MOVE L,(gotoX, gotoY , moveHeight, RX, RY, RZ), SPEED = 25, ACCEL = 1, DECEL = 1
			end if		
		endif
		FLUSH #intCamCom
		PRINT #intCamCom "(R,completed)"
	end if
Wend

'Return arm to controller for next program
GIVEARM

END
