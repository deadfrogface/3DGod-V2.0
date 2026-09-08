#!/usr/bin/env python3
"""3dgod-worker/1 echo worker for protocol tests."""
import json
import sys
import time
import traceback

PROTOCOL = "3dgod-worker/1"
MAX_LINE = 1024 * 1024


def send(obj):
    sys.stdout.write(json.dumps(obj, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def main():
    send({"v": PROTOCOL, "type": "hello", "worker": "echo"})
    cancelled = set()
    for raw in sys.stdin:
        if len(raw.encode("utf-8")) > MAX_LINE:
            send({"v": PROTOCOL, "type": "error", "code": "OversizedLine", "message": "line too long"})
            continue
        try:
            msg = json.loads(raw)
        except json.JSONDecodeError as exc:
            send({"v": PROTOCOL, "type": "error", "code": "InvalidJson", "message": str(exc)})
            continue
        kind = msg.get("type")
        if kind == "shutdown":
            break
        if kind == "cancel":
            cancelled.add(msg.get("id"))
            send({"v": PROTOCOL, "type": "error", "id": msg.get("id"), "code": "Cancelled", "message": "cancelled"})
            continue
        if kind != "request":
            send({"v": PROTOCOL, "type": "error", "code": "UnknownType", "message": str(kind)})
            continue
        req_id = msg.get("id")
        method = msg.get("method")
        if method == "crash":
            sys.exit(2)
        if method == "hang":
            time.sleep(3600)
        if method == "throw":
            try:
                raise RuntimeError("intentional-python-exception")
            except Exception:
                send(
                    {
                        "v": PROTOCOL,
                        "type": "error",
                        "id": req_id,
                        "code": "PythonException",
                        "message": "intentional-python-exception",
                        "diagnostics": {
                            "exceptionType": "RuntimeError",
                            "traceback": traceback.format_exc(),
                            "stage": "echo.throw",
                            "lastSuccessfulStage": "hello",
                        },
                    }
                )
                continue
        send({"v": PROTOCOL, "type": "progress", "id": req_id, "percent": 50})
        send(
            {
                "v": PROTOCOL,
                "type": "result",
                "id": req_id,
                "ok": True,
                "data": msg.get("params") or {},
            }
        )


if __name__ == "__main__":
    main()
