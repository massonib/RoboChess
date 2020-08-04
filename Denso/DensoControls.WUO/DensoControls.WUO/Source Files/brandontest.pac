'!TITLE "<Title>"
PROGRAM BrandonTest

'Variables
DEFINT intCamCom = 8
DEFINT intConState =-1


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
DEFINT iNum = 0
While iNum < 1000
	PRINTDBG "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0"
	PRINT #intCamCom "0" + "," + "0" + "," + "0" + "," + "0" + "," + "0"
	iNum = iNum + 1
	Delay 1000
Wend


MOTOR ON
SPEED 100
Move P, P[55]
Delay 10000

END
