"""Package tracked files from a Git commit and verify public release contents."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import zipfile


def validate(archive: Path) -> int:
    forbidden_dirs = {"local-profile", "local-backup", "runtime", ".git", ".gradle", "__pycache__"}
    forbidden_names = {"accounts.json", "local-config.json", "Config.Local.props", "Config.Build.user.props",
                       "Assembly-CSharp.dll", "steam_api.dll", "steam_api64.dll", "java.exe", "javaw.exe"}
    with zipfile.ZipFile(archive) as bundle:
        damaged = bundle.testzip()
        if damaged:
            raise ValueError("ZIP CRC failed: " + damaged)
        names = bundle.namelist()
        for name in names:
            path = PurePosixPath(name)
            if path.is_absolute() or ".." in path.parts or "\\" in name:
                raise ValueError("Unsafe archive path: " + name)
            if forbidden_dirs.intersection(path.parts) or path.name in forbidden_names:
                raise ValueError("Private/runtime/game file in release: " + name)
            if path.suffix.lower() == ".dll" and name not in {"binarios/PeakCraftCoop.dll", "binarios/aeironnarmiento.PeakCraft.dll"}:
                raise ValueError("Unexpected DLL; game/runtime assemblies must not be redistributed: " + name)
        expected = json.loads(bundle.read("PROVAS/checksums.json"))
        if not isinstance(expected, dict) or len(expected) < 50:
            raise ValueError("Expected at least 50 recorded mod/proof checksums")
        for name, digest in expected.items():
            if not isinstance(name, str) or not isinstance(digest, str) or not re.fullmatch(r"[0-9a-f]{64}", digest):
                raise ValueError("Invalid checksum entry")
            actual = hashlib.sha256(bundle.read(name)).hexdigest()
            if actual != digest:
                raise ValueError("Checksum mismatch: " + name)
        return len(expected)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ref", default="HEAD", help="Git commit/ref to package (default HEAD)")
    parser.add_argument("--output-dir", type=Path, default=Path("dist"))
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[1]
    metadata = json.loads((project / ".github/release.json").read_text(encoding="utf-8-sig"))
    version = metadata["version"]
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version) or metadata["tag"] != "v" + version:
        raise ValueError("Release version/tag mismatch")
    git = shutil.which("git")
    if git is None:
        raise RuntimeError("Git is required to package the exact tracked commit")
    commit = subprocess.check_output([git, "-C", str(project), "rev-parse", "--verify", args.ref + "^{commit}"], text=True).strip()
    output = args.output_dir.resolve()
    output.mkdir(parents=True, exist_ok=True)
    archive = output / ("MinePeak-" + version + ".zip")
    # Archive Git blobs with LF on every platform. Local caches, worlds and account
    # configuration are untracked and cannot enter this package.
    subprocess.run([git, "-C", str(project), "-c", "core.autocrlf=false", "archive", "--format=zip", "-o", str(archive), commit], check=True)
    count = validate(archive)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    checksum = archive.with_suffix(".zip.sha256")
    checksum.write_text(digest + "  " + archive.name + "\n", encoding="utf-8", newline="\n")
    print(json.dumps({"version": version, "commit": commit, "archive": str(archive), "bytes": archive.stat().st_size,
                      "proof_checksums_passed": count, "sha256": digest, "crc": "PASS"}, ensure_ascii=True))


if __name__ == "__main__":
    main()
