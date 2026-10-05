"""Bounded workers and socket timeouts for the loopback player gateway."""
from http.server import ThreadingHTTPServer as BaseServer
import threading

class ThreadingHTTPServer(BaseServer):
    daemon_threads = True
    def __init__(self, *args, **kwargs):
        self.workers = threading.BoundedSemaphore(64)
        super().__init__(*args, **kwargs)
    def process_request(self, request, address):
        request.settimeout(60)
        if not self.workers.acquire(blocking=False):
            try:
                request.settimeout(1)
                body = b'{"error":"Gateway busy"}'
                request.sendall(b'HTTP/1.1 503 Service Unavailable\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: ' + str(len(body)).encode() + b'\r\n\r\n' + body)
            except OSError:
                pass
            finally:
                self.shutdown_request(request)
            return
        try:
            super().process_request(request, address)
        except Exception:
            self.workers.release()
            raise
    def process_request_thread(self, request, address):
        try:
            super().process_request_thread(request, address)
        finally:
            self.workers.release()
