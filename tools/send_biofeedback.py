"""Send synthetic biofeedback packets for local Unity integration testing."""
import argparse
import math
import socket
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host', default='127.0.0.1')
    parser.add_argument('--port', type=int, default=8080)
    parser.add_argument('--bpm', type=float, default=78)
    parser.add_argument('--temperature', type=float, default=26.5)
    parser.add_argument('--humidity', type=float, default=48)
    parser.add_argument('--duration', type=float, default=10)
    parser.add_argument('--interval', type=float, default=0.05)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error('port must be 1..65535')
    if not math.isfinite(args.bpm) or not 0 <= args.bpm <= 260:
        parser.error('bpm must be finite and within 0..260')
    if not all(math.isfinite(x) for x in (args.temperature, args.humidity)):
        parser.error('environment values must be finite')
    if not all(math.isfinite(x) and x > 0 for x in (args.duration, args.interval)):
        parser.error('duration and interval must be positive finite numbers')
    packet = f'{args.bpm:g},{args.temperature:.2f},{args.humidity:.2f}'.encode('utf-8')
    print(f'SYNTHETIC TEST DATA -> {args.host}:{args.port}: {packet.decode()}')
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as client:
        deadline = time.monotonic() + args.duration
        while time.monotonic() < deadline:
            client.sendto(packet, (args.host, args.port))
            time.sleep(args.interval)


if __name__ == '__main__':
    main()
