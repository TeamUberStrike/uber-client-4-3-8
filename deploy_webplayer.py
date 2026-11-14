#!/usr/bin/env python3
import os
import posixpath
import shutil
import subprocess
import sys
import tempfile

ENV_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env")


def is_map_file(name):
    return name.startswith("Map-") and name.endswith(".unity3d")


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


def upload_via_sftp(host, port, user, password, local_dir, remote_path, htaccess_src):
    import paramiko

    transport = paramiko.Transport((host, int(port)))
    transport.connect(username=user, password=password)
    sftp = paramiko.SFTPClient.from_transport(transport)

    def ensure_dir(path):
        try:
            sftp.stat(path)
        except IOError:
            ensure_dir(posixpath.dirname(path))
            sftp.mkdir(path)

    ensure_dir(remote_path)

    for root, dirs, files in os.walk(local_dir):
        rel = os.path.relpath(root, local_dir)
        remote_root = posixpath.join(remote_path, rel) if rel != "." else remote_path
        ensure_dir(remote_root)
        for name in files:
            if is_map_file(name):
                print(f"skipping map {name}")
                continue
            local = os.path.join(root, name)
            remote = posixpath.join(remote_root, name)
            prefix = rel + "/" if rel != "." else ""
            print(f"uploading {prefix}{name}")
            sftp.put(local, remote)

    print("uploading .htaccess")
    sftp.put(htaccess_src, posixpath.join(remote_path, ".htaccess"))

    sftp.close()
    transport.close()


def main():
    env = load_env()
    repo_root = os.path.dirname(os.path.abspath(__file__))
    local_dir = env.get("WEBPLAYER_DIR", os.path.join(repo_root, "UberStrike.Unity", "Latest", "WebPlayer"))
    htaccess_src = os.path.join(repo_root, "content-server.htaccess")
    remote_path = env["REMOTE_WEBPLAYER_PATH"]
    host = env["REMOTE_HOST"]
    port = env.get("REMOTE_PORT", "22")
    user = env["REMOTE_USER"]
    password = env.get("REMOTE_PASSWORD", "")
    scp_opts = env.get("SCP_OPTS", "")

    if not os.path.isdir(local_dir):
        sys.exit(f"no WebGL build in {local_dir}")
    if not os.path.isfile(htaccess_src):
        sys.exit(f"missing {htaccess_src}")

    remote = f"{user}@{host}:{remote_path}"
    print(f"uploading {local_dir} -> {remote}")

    if password:
        upload_via_sftp(host, port, user, password, local_dir, remote_path, htaccess_src)
    else:
        staging = tempfile.mkdtemp(prefix="webplayer-staging-")
        try:
            shutil.copytree(local_dir, staging, dirs_exist_ok=True, ignore=shutil.ignore_patterns("Map-*.unity3d"))

            cmd = ["scp", "-r", "-P", port]
            if scp_opts:
                cmd.extend(scp_opts.split())
            cmd.extend([os.path.join(staging, "."), remote])
            subprocess.check_call(cmd)
        finally:
            shutil.rmtree(staging, ignore_errors=True)

        print("uploading .htaccess")
        cmd = ["scp", "-P", port]
        if scp_opts:
            cmd.extend(scp_opts.split())
        cmd.extend([htaccess_src, f"{user}@{host}:{posixpath.join(remote_path, '.htaccess')}"])
        subprocess.check_call(cmd)


if __name__ == "__main__":
    main()
