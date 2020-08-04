'!TITLE "tcp_cmd"

#include "tcp_lib.h"

PROGRAM TCP_Cmd(Channel as Integer, Cmd as integer, CamCmd as String, Row as string, ErrCode as Integer, Rslt0 as string, Rslt1 as string)
   defint IsServerChannel		'0=Client, 1=Server
   defint ChannelStatus			'-1 if not connected

   ErrCode = TCPLIB_ERR_NONE

   if (Channel < 4) or (Channel > 15) then
      'Channel is not a TCP Channel
      ErrCode = TCPLIB_ERR_BAD_CHANNEL
	  end
   endif

   if (Channel > 3) and (Channel < 8) then
      IsServerChannel = 1
   else
      IsServerChannel = 0
   endif


   select case Cmd
      case TCPLIB_CMD_CONNECT
	     'Connect
         if IsServerChannel = 1 then
            ChannelStatus = -1
		    while ChannelStatus < 0
               COM_STATE #Channel, ChannelStatus	'Wait for incoming connection
			wend
            FLUSH #Channel			'Clear the input buffer
		 else
		    COM_DISCOM #Channel	'Disconnect Communications to Camera
            DELAY 250
            COM_ENCOM #Channel		'Re-Connect to Camera (Server)
            FLUSH #Channel			'Clear the input buffer
		 endif

      case TCPLIB_CMD_DISCONNECT
	     'Disconnect
		 if IsServerChannel = 0 then
            COM_DISCOM #Channel	'Disconnect Communications to Camera
		 endif
         
      case TCPLIB_CMD_SEND
	      'Send Device Command
         flush #Channel
         PRINT #Channel, CamCmd

      case TCPLIB_CMD_RECV
	     'Recv Device Response
         INPUT #Channel, Rslt0, Rslt1

      case TCPLIB_CMD_QUERY
	     'Query Device Transaction (Send/Recv)
         PRINT #Channel, CamCmd
         LINEINPUT #Channel, CamCmd
      case else
	     'Cmd out of range
         ErrCode = TCPLIB_ERR_BAD_CMD
   end select
END
