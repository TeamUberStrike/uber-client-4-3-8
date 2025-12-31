# UberStrike Client 4.3.8

## License
This project is licensed under the GNU General Public License v3.0 (GPL-3.0). See the LICENSE file for details.

## Setup
Install Unity 3.5.5 from https://discussions.unity.com/t/early-unity-versions-downloads/927331

This Unity version produces 32bit binaries. This means apart from 64bit, it also runs on 32bit hardware/software supported devices like old Macs.

Inside the Unity Editor Latest.unity is used. Outside the Editor, running the application Spaceship.unity is the entry point.

Counterpart is the server at https://github.com/TeamUberStrike/uber-server-4-3-8

### Local Photon Server
You can choose to use local Photon Game/Comm servers instead of configuring this in the database.
Turn isEnabled on:
```
./UberStrike.Unity/Assets/Scenes/Latest.unity:8127:  _localGameServer:
./UberStrike.Unity/Assets/Scenes/Latest.unity:8131:  _localCommServer:
```

