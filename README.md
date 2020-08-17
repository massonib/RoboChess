# Video Link
https://youtu.be/fTlt4hATUtg

# RoboChess
Once installed, the camera, robot, and digital control unit will need to be setup using TCP. 

The digital control unit currently uses EasyModbus. To use a different protocol, change the Moxa.cs class and the line that initiates it in the MainViewModel constructor (Moxa = new Moxa(502, "10.2.10.15").

The TCP connections for the robot are fairly simple. Just make sure the clients (robot & camera) are pointed to your machine's IP address and the correct port. The port and address must match the properties in the MainViewModel.cs file.
The server will send a 0 to reset the clients, and the clients should return their type ("Camera" or "Robot"). 

Camera Triggers
0. Initialize/reset the camera (must have "Camera" in return string)
1. Trigger the camera and return the squares that changed on the board. These should be sent in order of most likely change (i.e. largest blob area in my implenentation). Some noise in not an issue.

Robot Actions IDs. Once each action is complete, the robot should return "completed." 
0. Initialize/reset the robot to home (must have "Robot" in return string)
1. Move the arm to given rank/file
2. Lower the arm
3. Raiser the arm
4. Move the arm to the drop-off location
5. Move the arm to the queen on the side of the board

# Contributions
Base of program from Meshack Musundi's StockChess project https://www.codeproject.com/Articles/1159746/StockChess

KTM Research hardware and facilities https://ktmresearch.com/

# License
https://www.codeproject.com/info/cpol10.aspx
