# RaiSimUnity

![raisimunity gif](Images/raisimunity.gif)
 
RaiSimUnity is a visualizer for RaiSim based on [Unity](https://unity.com/). It gets the simulation data from raisim server application via TCP/IP.

The project was tested on Ubuntu 18.04 LST, Windows10 and MacOS.

## Dependencies

The following Unity plugins are already included in the project.                
- [SimpleFileBrowser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)

The following Unity packages are imported by Unity Package Manager automatically: see [Packages/manifest.json](Packages/manifest.json) 
- [UnityMeshImporter](https://github.com/eastskykang/UnityMeshImporter)

- minizip ```sudo apt install minizip```

- vulkan. check https://linuxconfig.org/install-and-test-vulkan-on-linux

The followings are optional dependencies
- [ffmpeg](https://www.ffmpeg.org/) for video recording
    - You can install ffmpeg by 
        ```sh
        $ sudo apt install ffmpeg
        ``` 

## How to 

### Using RaiSimUnity

For beginners, we recommend downloading a compiled app image from the [raisimlib repo](https://github.com/raisimTech/raisimlib). 
This is stand-alone application thus you don't have to mind about dependencies or compiling. (only ffmpeg is required for recording a screen capture video.)

### Quickstart with RaiSim

1. Add the following line in your RaiSim simulation code: see [Example code](https://github.com/leggedrobotics/raisimUnity/tree/master/Examples/src)
    ```cpp
      /// launch raisim server
      raisim::RaisimServer server(&world);
      server.launchServer();
    
      while(1) {
        raisim::MSLEEP(2);
        server.integrateWorldThreadSafe();
      }
    
      server.killServer();
    ```
2. Run your RaiSim simulation. 
3. Run RaiSimUnity application.
![](Images/step1.png)
4. Add your resource directory that contains your mesh, material etc.
![](Images/step2.png)
5. Tap *Connect* button after specify TCP address and port.
![](Images/step3.png)
6. You can change background by *Background* dropdown menu in run time.
![](Images/step4.png)

### Development

The following sections introduces how to develop RaiSimUnity on Linux (Ubuntu 18.04 LTS). For more details, see [Installation wiki](https://github.com/leggedrobotics/RaiSimUnity/wiki/installation) and [Developing wiki](https://github.com/leggedrobotics/RaiSimUnity/wiki/developing).

## FAQ

- Is RaiSimUnity a stand-alone simulator? 
    - No, RaiSimUnity is a TCP client of [RaiSim](https://github.com/raisimtech/raisimLib).
- Is RaiSimUnity open-sourced?
    - Yes. However note that RaiSim is not an open-sourced project. 
- Can RaiSim and RaiSimUnity run on different machines? 
    - Yes. You can run RaiSim application on your "Server" machine and connect RaiSimUnity to the server by specifying IP address. 
- Does RaiSimUnity support Mac or Windows?
    - Yes
