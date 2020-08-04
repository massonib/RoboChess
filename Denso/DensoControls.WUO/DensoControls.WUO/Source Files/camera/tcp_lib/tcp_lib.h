
'  TCP_Lib.h

'TCPCmd Commands
#DEFINE TCPLIB_CMD_DISCONNECT    0     'Disconnect Channel
#DEFINE TCPLIB_CMD_CONNECT       1     'Connect Channel
#DEFINE TCPLIB_CMD_SEND          2     'Send Device Message
#DEFINE TCPLIB_CMD_RECV          3     'Recv Device Message (Non-Blocking)
#DEFINE TCPLIB_CMD_QUERY         4     'Query Device Transaction (Blocking)


'Error Codes
#DEFINE TCPLIB_ERR_NONE           0    'No Errors
#DEFINE TCPLIB_ERR_BAD_CHANNEL   -1    'Comm Channel Out of Range
#DEFINE TCPLIB_ERR_BAD_CMD       -2    'Invalid Command "Cmd"
