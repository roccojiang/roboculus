import os
import socket
import sys

import zipfile
import tqdm

OCULUS_IP = "146.169.154.222"
PORT = 5002
BUFFER_SIZE = 4096
SEPARATOR = ":"
END_SEPARATOR = "$"


def main():

    args = sys.argv[1:]

    if len(args) != 1 or not os.path.isdir(args[0]):
        print("Please choose a single folder with the URDF and meshes")
        return

    dirPath = args[0]
    dirName = os.path.basename(dirPath)

    s = socket.socket()

    print(f"\n[+] Connecting to Oculus at {OCULUS_IP}:{PORT}")
    s.connect((OCULUS_IP, PORT))
    print("[+] Connected")

    print("\n[+] Zipping folder")
    zipped = f"{dirName}.zip"
    with zipfile.ZipFile(zipped, "w", zipfile.ZIP_DEFLATED) as zip:
        for root, dirs, files in os.walk(dirPath):
            for file in files:
                zip.write(os.path.join(root, file), arcname=os.path.relpath(os.path.join(root, file), dirPath))

    print("\nSending file to Oculus...")
    send_file(zipped, s, dirName)

    s.close()

    print(f"\nSent {zipped} to Oculus!\n")
    print("\n[+] Deleting zip file")
    os.remove(zipped)


def send_file(file, socket, writeFileName):

    filesize = os.path.getsize(file)
    # send filename:filesize so server can open and write to file
    socket.send(f"{writeFileName}{SEPARATOR}{filesize}{END_SEPARATOR}".encode("utf-8"))

    progress = tqdm.tqdm(range(
        filesize), f"Sending {file}", unit="B",
        unit_scale=True, unit_divisor=1024)

    with open(file, "rb") as f:
        while True:
            bytes_read = f.read(BUFFER_SIZE)
            if not bytes_read:
                break
            socket.sendall(bytes_read)
            progress.update(len(bytes_read))


if __name__ == "__main__":
    main()
