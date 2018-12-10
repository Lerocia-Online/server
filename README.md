# Multiplayer Project

Multiplayer Project is an online multiplayer game built using the Unity3D engine. Players all connect to one world that they can explore together, collect items and weapons, and battle each other!

## Getting Started - User

These instructions will get you a copy of the production version of the game up and running for you to enjoy!

### Supported Operating Systems

Windows 7 SP1+
macOS 10.11+

### Downloading and Running

Visit [http://www.lerocia.com/](http://www.lerocia.com/) for the client zip downloads.
Once the download has finished, unzip the file and run `ClientBuild` to launch the game.

## Getting Started - Developer

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on the live servers.

### Installing

First you'll need to clone the repository. Navigate to the directory you want to keep the project in and run the following command to clone with HTTPS.

```
git clone https://github.com/JeffreyThor/Multiplayer-Project.git
```

Now there's one file you'll need to create in the `Assets/Scripts` directory of the repository you just cloned and call it `NetworkSettings.cs`. Now this file won't be tracked by git assuming you've named it properly so you don't need to worry about that, but here are the contents you should copy and paste into that file.

```
public static class NetworkSettings {  
  public const string ADDRESS = "127.0.0.1";  
  public const int PORT = 7777;  
}
```

Now if you have the address and port of one of the game servers then you would put that here if you wish to test against an existing server, but these steps will continue as if you're running everything locally.

Next you'll need to install the Unity editor. This project uses Unity version 2018.2.5f1. Go to The [Unity download archives](https://unity3d.com/get-unity/download/archive) and download the Unity 2018.2.5f1 Installer for your operating system.

When you launch Unity then open an existing project and navigate to the repository you cloned in the first step. You should now have everything to need to edit and test this project, but assuming you're running both client and server locally you'll need to generate a server build if you want a client to connect from your editor window.

### Testing

In order to run this game locally you'll need to have a server build running to connect to. To do this first open the server scene by going to `Assets/Scenes` in the project tab and double clicking `Server`. Then go to `file -> build settings` and click `Add Open Scenes`. Next click the check box next to the `Scenes/Server` option that appeared, make sure your target platform is set to your current operating system, then click `Build And Run`.

Once that build starts then your local server is up and running! Next just switch to the Client scene (or whatever the main scene is that isn't the server) in your editor and hit the play button. If you'd like to connect multiple clients at once you'll need to create and run multiple client builds since only one can run in the editor at a time.

## Deployment

If you have write access to this repository then any changes pushed to the development or master branches will kick off a new build and update the running server build and client download links. Pushes to development only update development builds and pushes to master only update production builds. The master branch is protected and requires administrative approval to merge into, if you would like to check on the status of builds after pushing new code just refer to the status section below.

## Status

[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project)](#) - Server  
[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project%20-%20Mac)](#) - Mac  
[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project%20-%20Windows)](#) - Windows  
[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project%20-%20Development)](#) - Development Server  
[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project%20-%20Mac%20-%20Development)](#) - Development Mac  
[![](http://18.205.119.15:8080/buildStatus/icon?job=Multiplayer%20Project%20-%20Windows%20-%20Development)](#) - Development Windows

## Built With

* [Unity3D](https://unity3d.com/) - Game engine
* [AWS EC2](https://aws.amazon.com/ec2/) - Build and game servers
* [AWS S3](https://aws.amazon.com/s3/) - Build storage
* [AWS RDS](https://aws.amazon.com/rds/) - Database service
* [MySQL](https://www.mysql.com/) - RDBMS
* [Jenkins](https://jenkins.io/) - Automation and CI

## Authors

* **Jeffrey Thor**

See also the list of [contributors](https://github.com/JeffreyThor/Multiplayer-Project/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details