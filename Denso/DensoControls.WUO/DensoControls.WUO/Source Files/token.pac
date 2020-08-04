'!TITLE "<Title>"
PROGRAM token(sINPUT AS STRING, sDELIMITER AS STRING, aDATA$(25))


'***********************************PROGRAM INFORMATION***************************************************
' Program Provided By:        KTM Research, LLC
' Client:            		  Nike IHM
' Revision:          		  18
' Rev Date:          		  8-29-2017
' Notes:             		  Initial release to Nike
' Contact Info:      		  david@ktmresearch.com
'                    		  www.ktmresearch.com
'***********************************PROGRAM INFORMATION***************************************************


'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************
' NOTE: This program is used to unpack strings into arrays.
'***********************************PROGRAM WARNINGS, CAUTIONS, AND NOTES***************************************************


DEFINT iLENGTH, iLOOPCOUNT, iARRAYCOUNT, iCHR1, iCHR2, iDATAPOS, iDATALEN
DEFSTR sTEMP ', sDELIMITER = ","

DIM aDELIMITERS%(24)
'DIM aDATA$(25)

LET iLOOPCOUNT = 1
LET iARRAYCOUNT = 0
iLENGTH = LEN (sINPUT)
iCHR1 = ASC(sDELIMITER)

'********************
'*	Locate Commas and record in array "aDELIMITERS"
'********************


DO UNTIL iLOOPCOUNT >= iLENGTH

	sTEMP = MID$(sINPUT,iLOOPCOUNT,1)
	iCHR2 = ASC(sTEMP)
	
	IF iCHR2 = iCHR1 THEN
		aDELIMITERS(iARRAYCOUNT) = iLOOPCOUNT
		iARRAYCOUNT = iARRAYCOUNT + 1
	END IF

	iLOOPCOUNT = iLOOPCOUNT + 1
LOOP

'********************
'*	Use recorded Delimiter locations, divide data string & record to aDATA
'********************

LET iLOOPCOUNT = 0

DO UNTIL iLOOPCOUNT > iARRAYCOUNT

	IF iLOOPCOUNT = 0 THEN
		iDATAPOS = 0
		iDATALEN = aDELIMITERS(iLOOPCOUNT) - 1
	ELSEIF iLOOPCOUNT = iARRAYCOUNT THEN
		iDATAPOS = aDELIMITERS(iLOOPCOUNT - 1) + 1
		iDATALEN = iLENGTH - aDELIMITERS(iLOOPCOUNT - 1)
	ELSE
		iDATAPOS = aDELIMITERS(iLOOPCOUNT - 1) + 1
		iDATALEN = aDELIMITERS(iLOOPCOUNT) - aDELIMITERS(iLOOPCOUNT - 1) - 1
	END IF
	
	aDATA(iLOOPCOUNT) = MID$(sINPUT, iDATAPOS, iDATALEN)
	
	iLOOPCOUNT = iLOOPCOUNT +1
LOOP

END
