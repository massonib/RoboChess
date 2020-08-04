''!TITLE "Camera"
'
'#include "camera.h"
'
'PROGRAM Camera(Command as integer, Parameters as string, ReturnValues as string, ErrCode as integer)
'   'This program encapsulates communications with the camera
'   dim TcpErrCode as integer
'   dim CommandString as string
'
'   'Initialize Retrun Code
'   ErrCode=CAM_ERR_NO_ERROR
'
'   select case Command
'      case CAM_CMD_CONNECT
'         'Connect to Camera
'         call tcp_cmd(CAM_SET_COM_CHANNEL, "Connect", "", TcpErrCode)
'         if TcpErrCode<0
'            ErrCode=CAM_ERR_CONNECT_FAIL
'         endif
'      case CAM_CMD_DISCONNECT
'         'Disconnect from Camera
'         call tcp_cmd(CAM_SET_COM_CHANNEL, "Disconnect", "", TcpErrCode)
'         if TcpErrCode<0
'            ErrCode=CAM_ERR_DISCONNECT_FAIL
'         endif
'
'      case CAM_CMD_CALIBRATE
'         'Execute 'test1' camera function
'         CommandString = "CAL:" + Parameters
'         call tcp_cmd(CAM_SET_COM_CHANNEL, "Query", CommandString, TcpErrCode)
'         if TcpErrCode<0
'            ErrCode=CAM_ERR_UNKNOWN
'         endif
'         ReturnValues=CommandString
'
'
'      case else
'         ErrCode = CAM_ERR_INVALID_CMD
'   end select
'
'END
'
