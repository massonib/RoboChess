'!TITLE "<Connect>"

#include "tcp_lib\tcp_lib.h"
#include "camera.h"

PROGRAM Connect
   extern dim Cam_ErrorCode as integer
   extern dim Cam_Connected as integer

   dim TcpErrCode as integer

   call tcp_lib.tcp_cmd(CAM_PARAM_COM_CHANNEL, TCPLIB_CMD_CONNECT, "","", TcpErrCode, "", "")

   if TcpErrCode=TCPLIB_ERR_NONE
      Cam_Connected=1
      Cam_ErrorCode=CAM_ERR_NO_ERROR
   else
      Cam_ErrorCode=CAM_ERR_CONNECT_FAIL
   endif
END
