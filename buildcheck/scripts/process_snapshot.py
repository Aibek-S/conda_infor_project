import ctypes
import datetime as dt
import json
import platform
from ctypes import wintypes

import psutil


def utc_now_iso():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def enum_visible_windows():
    user32 = ctypes.windll.user32
    windows = []

    enum_windows_proc = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)

    def callback(hwnd, _lparam):
        if not user32.IsWindowVisible(hwnd):
            return True

        length = user32.GetWindowTextLengthW(hwnd)
        if length <= 0:
            return True

        buffer = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buffer, length + 1)
        title = buffer.value.strip()
        if not title:
            return True

        pid = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))

        windows.append(
            {
                "hwnd": int(hwnd),
                "pid": int(pid.value),
                "title": title,
            }
        )
        return True

    user32.EnumWindows(enum_windows_proc(callback), 0)
    return windows


def collect_visible_window_titles():
    titles = []
    seen = set()
    for window in enum_visible_windows():
        title = (window.get("title") or "").strip()
        if not title:
            continue

        key = title.lower()
        if key in seen:
            continue

        seen.add(key)
        titles.append(title)

    return titles


def get_active_window_title():
    user32 = ctypes.windll.user32
    hwnd = user32.GetForegroundWindow()
    if not hwnd:
        return ""

    length = user32.GetWindowTextLengthW(hwnd)
    if length <= 0:
        return ""

    buffer = ctypes.create_unicode_buffer(length + 1)
    user32.GetWindowTextW(hwnd, buffer, length + 1)
    return buffer.value.strip()


def collect_process_names():
    names = []
    seen = set()

    for proc in psutil.process_iter(["name"]):
        try:
            name = (proc.info.get("name") or "").strip()
            if not name:
                continue

            key = name.lower()
            if key in seen:
                continue

            seen.add(key)
            names.append(name)
        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
            continue

    return names


def build_payload():
    try:
        return {
            "activeWindow": get_active_window_title(),
            "windows": collect_visible_window_titles(),
            "processes": collect_process_names(),
            "debug": {
                "source": "python-collector",
                "message": "ok",
                "generatedAtUtc": utc_now_iso(),
                "host": platform.node(),
            },
        }
    except Exception as exc:
        return {
            "activeWindow": "",
            "windows": [],
            "processes": [],
            "debug": {
                "source": "python-collector",
                "message": f"error: {type(exc).__name__}",
                "error": str(exc),
                "generatedAtUtc": utc_now_iso(),
            },
        }


def main():
    # C# reads stdout as a single JSON document. Do not print logs here.
    print(json.dumps(build_payload(), ensure_ascii=False))


if __name__ == "__main__":
    main()
