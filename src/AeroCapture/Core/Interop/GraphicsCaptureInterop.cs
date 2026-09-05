using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace AeroCapture.Core.Interop;

public static class GraphicsCaptureInterop
{
    [ComImport]
    [Guid("3E68A4BD-E2C3-4866-88B0-0E0C8F473B9F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
        IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
    }

    [ComImport]
    [Guid("3E68A4BD-E2C3-4866-88B0-0E0C8F473B9F")]
    public class GraphicsCaptureItemInteropHelper
    {
    }

    public static GraphicsCaptureItem? CreateItemForWindow(IntPtr hWnd)
    {
        try
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var guid = typeof(GraphicsCaptureItem).GUID;
            var raw = interop.CreateForWindow(hWnd, ref guid);
            return Marshal.GetObjectForIUnknown(raw) as GraphicsCaptureItem;
        }
        catch
        {
            return null;
        }
    }

    public static GraphicsCaptureItem? CreateItemForMonitor(IntPtr hMonitor)
    {
        try
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var guid = typeof(GraphicsCaptureItem).GUID;
            var raw = interop.CreateForMonitor(hMonitor, ref guid);
            return Marshal.GetObjectForIUnknown(raw) as GraphicsCaptureItem;
        }
        catch
        {
            return null;
        }
    }
}

internal static class WindowsRuntimeMarshal
{
    [DllImport("api-ms-win-core-winrt-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int RoGetActivationFactory(
        [MarshalAs(UnmanagedType.HString)] string activatableClassId,
        [In] ref Guid iid,
        [Out, MarshalAs(UnmanagedType.IUnknown)] out object factory);

    public static object GetActivationFactory(Type type)
    {
        string typeName = type.FullName ?? throw new ArgumentException("Invalid type name", nameof(type));
        Guid guid = typeof(GraphicsCaptureInterop.IGraphicsCaptureItemInterop).GUID;
        int hr = RoGetActivationFactory(typeName, ref guid, out object factory);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
        return factory;
    }
}
