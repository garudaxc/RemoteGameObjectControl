# RemoteGameObjectControl
####control game object in remote app from pc

This tool can let you active or deactive any game object in the scene running on a mobile device from PC.
This can be very useful when you are doing profiling and debuging.
Both your PC and mobile device need network connection to make it work.
<br><br>
##how to use:<br>
1.attach script `RemoteGameObjectControl.cs` to scene, set type to client, and input PC's IP address.<br>
2.build apk and install to devices<br>
3.creat an empty scene, attach script RemoteGameObjectControl.cs to scene, set type to server<br>
4.play scene on PC, then play app on mobile devices<br>
5.press "update" button on game view, all game object will be synchronized in the hierarchy view.<br>
6.select any game object, check or uncheck property "Active" in the Inspector view, it will affect corresponding game object in your mobile device.
