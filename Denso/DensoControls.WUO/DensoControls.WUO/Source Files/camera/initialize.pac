'!TITLE "<Titile>"

#include "camera.h"

PROGRAM Initialize
   extern dim Cam_ErrorCode as integer
   extern dim Cam_Connected as integer

   'Perform Initialization Actions
   Cam_ErrorCode=CAM_ERR_NO_ERROR
   Cam_Connected=0

END
