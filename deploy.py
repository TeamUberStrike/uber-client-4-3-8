#!/usr/bin/env python3
"""Deploy the WebGL webplayer, maps and images to All-Inkl.

Usage:
  ./deploy.py <dev|qa|production> [all|webplayer|maps|images]

Each mode resolves the remote paths from .env:
  REMOTE_CONTENT_<MODE>     content root (contains Maps/ and Images/)
  REMOTE_WEBPLAYER_<MODE>   webplayer docroot
  MAPS_VERSION              maps subfolder under the content root (default 4.3.8)
"""
import glob
import os
import posixpath
import shutil
import subprocess
import sys
import tempfile

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

MODES = ("dev", "qa", "production")


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


class Deployer:
    def __init__(self, env, mode):
        self.env = env
        self.mode = mode
        self.upper = mode.upper()
        self.host = env["REMOTE_HOST"]
        self.port = env.get("REMOTE_PORT", "22")
        self.user = env["REMOTE_USER"]
        self.password = env.get("REMOTE_PASSWORD", "")
        self.scp_opts = env.get("SCP_OPTS", "")

        repo_root = os.path.dirname(os.path.abspath(__file__))
        self.build_dir = env.get("BUILD_DIR", os.path.join(repo_root, "UberStrike.Unity", "Latest", "WebPlayer"))
        self.images_dir = env.get("IMAGES_DIR", os.path.join(repo_root, "images"))
        self.htaccess_src = os.path.join(repo_root, "content-server.htaccess")
        self.maps_version = env.get("MAPS_VERSION", "4.3.8")

        self.content_root = env.get(f"REMOTE_CONTENT_{self.upper}")
        self.webplayer_root = env.get(f"REMOTE_WEBPLAYER_{self.upper}")
        if not self.content_root or not self.webplayer_root:
            sys.exit(f"missing REMOTE_CONTENT_{self.upper} / REMOTE_WEBPLAYER_{self.upper} in {ENV_FILE}")

        self.maps_path = posixpath.join(self.content_root, "Maps", self.maps_version)
        self.images_path = posixpath.join(self.content_root, "Images")

    def _ensure_dir(self, sftp, path):
        try:
            sftp.stat(path)
        except IOError:
            parent = posixpath.dirname(path)
            if parent != path:
                self._ensure_dir(sftp, parent)
            sftp.mkdir(path)

    def _upload_dir(self, sftp, local_dir, remote_path, skip=None):
        for root, dirs, files in os.walk(local_dir):
            rel = os.path.relpath(root, local_dir)
            remote_root = posixpath.join(remote_path, rel) if rel != "." else remote_path
            self._ensure_dir(sftp, remote_root)
            for name in files:
                if skip and skip(name):
                    print(f"skipping {name}")
                    continue
                prefix = rel + "/" if rel != "." else ""
                print(f"uploading {prefix}{name}")
                sftp.put(os.path.join(root, name), posixpath.join(remote_root, name))

    def _put(self, sftp, src, dest):
        print(f"uploading {os.path.basename(dest)}")
        sftp.put(src, dest)

    def run(self, targets):
        if "all" in targets:
            targets = ["webplayer", "maps", "images"]

        if self.password:
            import paramiko

            transport = paramiko.Transport((self.host, int(self.port)))
            transport.connect(username=self.user, password=self.password)
            sftp = paramiko.SFTPClient.from_transport(transport)
            try:
                self._run_sftp(sftp, targets)
            finally:
                sftp.close()
                transport.close()
        else:
            self._run_scp(targets)

    def _run_sftp(self, sftp, targets):
        if "webplayer" in targets:
            print(f"== webplayer -> {self.user}@{self.host}:{self.webplayer_root}")
            self._upload_dir(sftp, self.build_dir, self.webplayer_root, skip=self._is_map_file)
            self._put(sftp, self.htaccess_src, posixpath.join(self.webplayer_root, ".htaccess"))

        if "maps" in targets:
            print(f"== maps -> {self.user}@{self.host}:{self.maps_path}")
            staging = tempfile.mkdtemp(prefix="maps-staging-")
            try:
                for map_id, level_name in MAPS.items():
                    candidates = glob.glob(os.path.join(self.build_dir, f"Map-{map_id:02d}-*.unity3d"))
                    if not candidates:
                        print(f"WARN: no build for map {map_id} ({level_name}) in {self.build_dir}")
                        continue
                    dst = os.path.join(staging, f"{level_name}.unity3d")
                    shutil.copy2(candidates[0], dst)
                    print(f"staged {os.path.basename(candidates[0])} -> {level_name}.unity3d")
                if not os.listdir(staging):
                    sys.exit("nothing to upload for maps")
                self._upload_dir(sftp, staging, self.maps_path)
            finally:
                shutil.rmtree(staging, ignore_errors=True)

        if "images" in targets:
            print(f"== images -> {self.user}@{self.host}:{self.images_path}")
            self._upload_dir(sftp, self.images_dir, self.images_path)
            self._put(sftp, self.htaccess_src, posixpath.join(self.content_root, ".htaccess"))

    def _run_scp(self, targets):
        def scp_sources(srcs, remote):
            cmd = ["scp", "-r", "-P", self.port]
            if self.scp_opts:
                cmd.extend(self.scp_opts.split())
            cmd.extend(srcs + [f"{self.user}@{self.host}:{remote}"])
            subprocess.check_call(cmd)

        if "webplayer" in targets:
            print(f"== webplayer -> {self.user}@{self.host}:{self.webplayer_root}")
            staging = tempfile.mkdtemp(prefix="webplayer-staging-")
            try:
                shutil.copytree(self.build_dir, staging, dirs_exist_ok=True, ignore=shutil.ignore_patterns("Map-*.unity3d"))
                scp_sources([os.path.join(staging, ".")], self.webplayer_root)
                scp_sources([self.htaccess_src], posixpath.join(self.webplayer_root, ".htaccess"))
            finally:
                shutil.rmtree(staging, ignore_errors=True)

        if "maps" in targets:
            print(f"== maps -> {self.user}@{self.host}:{self.maps_path}")
            staging = tempfile.mkdtemp(prefix="maps-staging-")
            try:
                for map_id, level_name in MAPS.items():
                    candidates = glob.glob(os.path.join(self.build_dir, f"Map-{map_id:02d}-*.unity3d"))
                    if not candidates:
                        print(f"WARN: no build for map {map_id} ({level_name}) in {self.build_dir}")
                        continue
                    dst = os.path.join(staging, f"{level_name}.unity3d")
                    shutil.copy2(candidates[0], dst)
                    print(f"staged {os.path.basename(candidates[0])} -> {level_name}.unity3d")
                if not os.listdir(staging):
                    sys.exit("nothing to upload for maps")
                scp_sources([os.path.join(staging, ".")], self.maps_path)
            finally:
                shutil.rmtree(staging, ignore_errors=True)

        if "images" in targets:
            print(f"== images -> {self.user}@{self.host}:{self.images_path}")
            staging = tempfile.mkdtemp(prefix="images-staging-")
            try:
                shutil.copytree(self.images_dir, staging, dirs_exist_ok=True)
                scp_sources([os.path.join(staging, ".")], self.images_path)
                scp_sources([self.htaccess_src], posixpath.join(self.content_root, ".htaccess"))
            finally:
                shutil.rmtree(staging, ignore_errors=True)

    @staticmethod
    def _is_map_file(name):
        return name.startswith("Map-") and name.endswith(".unity3d")


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in MODES:
        sys.exit(f"usage: {os.path.basename(__file__)} <dev|qa|production> [all|webplayer|maps|images]")

    mode = sys.argv[1]
    targets = sys.argv[2:] or ["all"]
    for t in targets:
        if t not in ("all", "webplayer", "maps", "images"):
            sys.exit(f"unknown target: {t}")

    env = load_env()
    deployer = Deployer(env, mode)
    print(f"deploying mode={mode} targets={targets}")
    print(f"  content root:   {deployer.content_root}")
    print(f"  webplayer root: {deployer.webplayer_root}")
    print(f"  maps path:      {deployer.maps_path}")
    print(f"  images path:    {deployer.images_path}")
    deployer.run(targets)


if __name__ == "__main__":
    main()
