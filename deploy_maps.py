#!/usr/bin/env python3
import glob
import os
import shutil
import subprocess
import sys

ENV_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env")

MAPS = {
    1: "LevelMonkeyIsland",
    2: "LevelLostParadise2",
    3: "LevelTheWarehouse",
    4: "LevelTempleOfTheRaven",
    5: "LevelFortWinter",
    6: "LevelGideonsTower",
    7: "LevelSkyGarden",
    8: "LevelCuberStrike",
    10: "LevelSpaceportAlpha",
}

def load_env():
    env = {}
    if not os.path.exists(ENV_FILE):
        sys.exit(f"missing {ENV_FILE}")
    with open(ENV_FILE) as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            env[k.strip()] = v.strip()
    return env

def upload_via_sftp(host, port, user, password, files, remote_path):
    import paramiko

    transport = paramiko.Transport((host, int(port)))
    transport.connect(username=user, password=password)
    sftp = paramiko.SFTPClient.from_transport(transport)
    for f in files:
        remote_file = f"{remote_path}/{os.path.basename(f)}"
        print(f"uploading {os.path.basename(f)}")
        sftp.put(f, remote_file)
    sftp.close()
    transport.close()

def main():
    env = load_env()
    build_dir = env["BUILD_DIR"]
    staging = env["STAGING_DIR"]
    remote = f"{env['REMOTE_USER']}@{env['REMOTE_HOST']}:{env['REMOTE_MAPS_PATH']}"
    port = env.get("REMOTE_PORT", "22")
    password = env.get("REMOTE_PASSWORD", "")
    scp_opts = env.get("SCP_OPTS", "")

    if os.path.exists(staging):
        shutil.rmtree(staging)
    os.makedirs(staging)

    for map_id, level_name in MAPS.items():
        candidates = glob.glob(os.path.join(build_dir, f"Map-{map_id:02d}-*.unity3d"))
        if not candidates:
            print(f"WARN: no build for map {map_id} ({level_name}) in {build_dir}")
            continue
        src = candidates[0]
        dst = os.path.join(staging, f"{level_name}.unity3d")
        shutil.copy2(src, dst)
        print(f"staged {os.path.basename(src)} -> {level_name}.unity3d")

    files = glob.glob(os.path.join(staging, "*.unity3d"))
    if not files:
        sys.exit("nothing to upload")

    print(f"uploading {len(files)} maps to {remote}")
    if password:
        upload_via_sftp(env["REMOTE_HOST"], port, env["REMOTE_USER"], password, files, env["REMOTE_MAPS_PATH"])
    else:
        cmd = ["scp", "-P", port]
        if scp_opts:
            cmd.extend(scp_opts.split())
        cmd.extend(files + [remote])
        subprocess.check_call(cmd)

if __name__ == "__main__":
    main()
