# UberStrike Client 4.3.8

## License
  This project is licensed under the GNU General Public License v3.0 (GPL-3.0). See the LICENSE file for details.

## Setup
- Install Unity Hub -> https://unity.com/download
- Add the repository in Unity Hub
- Unity Hub will automatically select the required Unity version

- Inside the Unity Editor Latest.unity is used. Outside the Editor, running the application Spaceship.unity is the entry point.

### Local Photon Server
  You can choose to use local Photon Game/Comm servers instead of configuring this in the database.
  Turn isEnabled on:
```
./UberStrike.Unity/Assets/Scenes/Latest.unity:8127:  _localGameServer:
./UberStrike.Unity/Assets/Scenes/Latest.unity:8131:  _localCommServer:
```
