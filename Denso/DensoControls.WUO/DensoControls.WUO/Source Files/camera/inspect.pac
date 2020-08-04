'!TITLE "<Title>"
#include "camera.h"
#include "tcp_lib\tcp_lib.h"

PROGRAM Inspect(cmd as string, InspResult as string)
'PROGRAM TCP_Cmd(Channel as Integer, Cmd as integer, CamCmd as String, col as string, ErrCode as Integer, Rslt0 as string, Rslt1 as string)

   defstr BINResult
   defint count
   
   extern dim Cam_ErrorCode as integer
   extern dim Cam_Connected as integer
   'extern dim Insp_Results(1,8) as integer

   extern dim TrayInspectionResult as integer      'Saves top view inspection results for RejectTips


   dim Rslt0 as string
   dim Rslt1 as string

   dim TcpErrCode as integer

'   'Clear the binary result string
'   BINResult=""
'   'Reset loop counter
'   count=0

   call tcp_lib.tcp_cmd(CAM_PARAM_COM_CHANNEL, TCPLIB_CMD_SEND, cmd, "0", TcpErrCode, "", "")

   call tcp_lib.tcp_cmd(CAM_PARAM_COM_CHANNEL, TCPLIB_CMD_RECV,"","", TcpErrCode, Rslt0, Rslt1)

   'BINResult=bin$(val(Rslt1))

   InspResult = Rslt1

'   select case Rslt0
' 
'      case "Tray"
'         'Insp_Results(0,0)=val(Rslt1)
'         TrayInspectionResult=val(Rslt1)
'
'      case "TipsL"
'         Insp_Results(0,val(col))=val(Rslt1)
'
''      case "TipsR"
'''         while count < len(BINResult)
'''            Insp_Results(0,count)=val(mid$(BINResult, count, 1))
'''            count=count+1
'''         wend
''         Insp_Results(0,val(col))=val(Rslt1)
'
'   end select

END
