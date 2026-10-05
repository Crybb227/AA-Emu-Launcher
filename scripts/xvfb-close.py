"""Send a normal window-close event only on the verification run's isolated Xvfb."""
import ctypes as c
import ctypes.util
import os

def close_test_windows(display):
    x = c.CDLL(ctypes.util.find_library('X11'))
    x.XOpenDisplay.argtypes = [c.c_char_p]; x.XOpenDisplay.restype = c.c_void_p
    d = x.XOpenDisplay(display.encode())
    if not d:
        return
    class Data(c.Union):
        _fields_ = [('b', c.c_char * 20), ('s', c.c_short * 10), ('l', c.c_long * 5)]
    class Message(c.Structure):
        _fields_ = [('type', c.c_int), ('serial', c.c_ulong), ('send_event', c.c_int), ('display', c.c_void_p), ('window', c.c_ulong), ('message_type', c.c_ulong), ('format', c.c_int), ('data', Data)]
    class Event(c.Union):
        _fields_ = [('message', Message), ('pad', c.c_long * 24)]
    x.XDefaultRootWindow.argtypes = [c.c_void_p]; x.XDefaultRootWindow.restype = c.c_ulong
    x.XQueryTree.argtypes = [c.c_void_p, c.c_ulong, c.POINTER(c.c_ulong), c.POINTER(c.c_ulong), c.POINTER(c.POINTER(c.c_ulong)), c.POINTER(c.c_uint)]
    x.XInternAtom.argtypes = [c.c_void_p, c.c_char_p, c.c_int]; x.XInternAtom.restype = c.c_ulong
    x.XSendEvent.argtypes = [c.c_void_p, c.c_ulong, c.c_int, c.c_long, c.POINTER(Event)]
    x.XFlush.argtypes = [c.c_void_p]; x.XCloseDisplay.argtypes = [c.c_void_p]; x.XFree.argtypes = [c.c_void_p]
    root, parent, count = c.c_ulong(), c.c_ulong(), c.c_uint()
    children = c.POINTER(c.c_ulong)()
    try:
        x.XQueryTree(d, x.XDefaultRootWindow(d), c.byref(root), c.byref(parent), c.byref(children), c.byref(count))
        for index in range(count.value):
            event = Event()
            event.message.type = 33
            event.message.display = d
            event.message.window = children[index]
            event.message.message_type = x.XInternAtom(d, b'WM_PROTOCOLS', 0)
            event.message.format = 32
            event.message.data.l[0] = x.XInternAtom(d, b'WM_DELETE_WINDOW', 0)
            x.XSendEvent(d, children[index], 0, 0, c.byref(event))
        x.XFlush(d)
    finally:
        if children:
            x.XFree(children)
        x.XCloseDisplay(d)
