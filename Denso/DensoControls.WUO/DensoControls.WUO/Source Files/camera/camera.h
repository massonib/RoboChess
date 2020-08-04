'!TITLE "@camera.h"

'This file contains #defines for use with camera.pac

'User Settings
#DEFINE CAM_PARAM_IP_ADDR         "192.168.10.3"
#DEFINE CAM_PARAM_COM_CHANNEL     8


'Error Codes
#DEFINE CAM_ERR_NO_ERROR          0
#DEFINE CAM_ERR_UNKNOWN          -1
#DEFINE CAM_ERR_CONNECT_FAIL     -2
#DEFINE CAM_ERR_DISCONNECT_FAIL  -3
#DEFINE CAM_ERR_CMD_FAILURE      -4    'Camera encountered error while processing command
#DEFINE CAM_ERR_BAD_ARG          -5    'Invalid Argument passed to command
