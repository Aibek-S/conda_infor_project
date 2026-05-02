import json


def main():
    # Rename this file to process_snapshot.py when the real collector is ready.
    # The C# app reads only stdout, so do not print logs before/after this JSON.
    payload = {
        "activeWindow": "Google Chrome - lesson page",
        "processes": [
            "chrome.exe",
            "notepad.exe",
            "explorer.exe"
        ],
        "debug": {
            "source": "python-example",
            "message": "Example collector output"
        }
    }

    print(json.dumps(payload, ensure_ascii=False))


if __name__ == "__main__":
    main()
