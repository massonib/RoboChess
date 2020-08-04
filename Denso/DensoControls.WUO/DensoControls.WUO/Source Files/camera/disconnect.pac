'!TITLE "<Disconnect>"

#include "tcp_lib\tcp_lib.h"
#include "camera.h"

PROGRAM Disconnect
   extern dim Cam_ErrorCode as integer
   extern dim Cam_Connected as integer

   dim TcpErrCode as integer

   'call tcp_lib.tcp_cmd(CAM_PARAM_COM_CHANNEL, TCPLIB_CMD_DISCONNECT)
   call tcp_lib.tcp_cmd(CAM_PARAM_COM_CHANNEL, TCPLIB_CMD_DISCONNECT, "", "", TcpErrCode, "", "")

   if TcpErrCode=TCPLIB_ERR_NONE
      Cam_Connected=0
      Cam_ErrorCode=CAM_ERR_NO_ERROR
   else
      Cam_ErrorCode=CAM_ERR_DISCONNECT_FAIL
   endif
END
