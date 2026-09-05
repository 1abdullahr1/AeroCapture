using System;
using System.Runtime.InteropServices;

namespace AeroCapture.Core.Interop;

public static class D3D11Interop
{
    public const int D3D11_SDK_VERSION = 7;
    public const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x0020;
    public const uint D3D11_CREATE_DEVICE_SINGLETHREADED = 0x0001;

    public const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    public const int DXGI_FORMAT_R8G8B8A8_UNORM = 28;

    public const int D3D11_USAGE_DEFAULT = 0;
    public const int D3D11_USAGE_STAGING = 3;
    public const int D3D11_CPU_ACCESS_READ = 0x00020000;
    public const int D3D11_MAP_READ = 1;

    public const int DXGI_ERROR_WAIT_TIMEOUT = unchecked((int)0x887A0027);
    public const int DXGI_ERROR_ACCESS_LOST = unchecked((int)0x887A0026);
    public const int DXGI_ERROR_ACCESS_DENIED = unchecked((int)0x887A002B);

    public enum D3D_DRIVER_TYPE
    {
        UNKNOWN = 0,
        HARDWARE = 1,
        REFERENCE = 2,
        NULL = 3,
        SOFTWARE = 4,
        WARP = 5
    }

    public enum D3D_FEATURE_LEVEL
    {
        Level_11_1 = 0xb100,
        Level_11_0 = 0xb000,
        Level_10_1 = 0xa100,
        Level_10_0 = 0xa000,
        Level_9_3 = 0x9300,
        Level_9_2 = 0x9200,
        Level_9_1 = 0x9100
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_OUTDUPL_FRAME_INFO
    {
        public long LastPresentTime;
        public long LastMouseUpdateTime;
        public uint TotalMetadataBuffersSize;
        public uint AccumulatedFrames;
        public bool RectsCoalesced;
        public bool ProtectedContentMaskedOut;
        public DXGI_OUTDUPL_POINTER_POSITION PointerPosition;
        public uint TotalPointerShapeBufferSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_OUTDUPL_POINTER_POSITION
    {
        public Win32.POINT Position;
        public bool Visible;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_TEXTURE2D_DESC
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public int Format;
        public DXGI_SAMPLE_DESC SampleDesc;
        public int Usage;
        public uint BindFlags;
        public uint CPUAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_MAPPED_SUBRESOURCE
    {
        public IntPtr pData;
        public uint RowPitch;
        public uint DepthPitch;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DXGI_OUTPUT_DESC
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        public Win32.RECT DesktopCoordinates;
        public bool AttachedToDesktop;
        public int Rotation;
        public IntPtr Monitor;
    }

    [DllImport("d3d11.dll", PreserveSig = false)]
    public static extern void D3D11CreateDevice(
        IntPtr pAdapter,
        D3D_DRIVER_TYPE DriverType,
        IntPtr Software,
        uint Flags,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] D3D_FEATURE_LEVEL[]? pFeatureLevels,
        uint FeatureLevels,
        uint SDKVersion,
        out IntPtr ppDevice,
        out D3D_FEATURE_LEVEL pFeatureLevel,
        out IntPtr ppImmediateContext);

    [ComImport]
    [Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID3D11Device
    {
        // Add basic interface slot offsets
        void CreateBuffer();
        void CreateTexture1D();
        [PreserveSig]
        int CreateTexture2D(ref D3D11_TEXTURE2D_DESC pDesc, IntPtr pInitialData, out IntPtr ppTexture2D);
    }

    [ComImport]
    [Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ID3D11DeviceContext
    {
        // Basic methods
        void VSSetConstantBuffers();
        void PSSetShaderResources();
        void PSSetShader();
        void PSSetSamplers();
        void VSSetShader();
        void DrawIndexed();
        void Draw();
        void Map();
        void Unmap();
        void PSSetConstantBuffers();
        void IASetInputLayout();
        void IASetVertexBuffers();
        void IASetIndexBuffer();
        void DrawIndexedInstanced();
        void DrawInstanced();
        void GSSetConstantBuffers();
        void GSSetShader();
        void IASetPrimitiveTopology();
        void CSSetShaderResources();
        void CSSetUnorderedAccessViews();
        void CSSetShader();
        void CSSetSamplers();
        void CSSetConstantBuffers();
        void VSSetShaderResources();
        void VSSetSamplers();
        void Begin();
        void End();
        void GetData();
        void SetPredication();
        void GSSetShaderResources();
        void GSSetSamplers();
        void OMSetRenderTargets();
        void OMSetRenderTargetsAndUnorderedAccessViews();
        void OMSetBlendState();
        void OMSetDepthStencilState();
        void SOSetTargets();
        void DrawAuto();
        void DrawIndexedInstancedIndirect();
        void DrawInstancedIndirect();
        void Dispatch();
        void DispatchIndirect();
        void RSSetState();
        void RSSetViewports();
        void RSSetScissorRects();
        [PreserveSig]
        void CopySubresourceRegion(IntPtr pDstResource, uint DstSubresource, uint DstX, uint DstY, uint DstZ,
            IntPtr pSrcResource, uint SrcSubresource, IntPtr pSrcBox);
        [PreserveSig]
        void CopyResource(IntPtr pDstResource, IntPtr pSrcResource);
        void UpdateSubresource();
        void CopyStructureCount();
        void ClearRenderTargetView();
        void ClearUnorderedAccessViewUint();
        void ClearUnorderedAccessViewFloat();
        void ClearDepthStencilView();
        void GenerateMips();
        void SetResourceMinLOD();
        void GetResourceMinLOD();
        void ResolveSubresource();
        void ExecuteCommandList();
        void HSSetShaderResources();
        void HSSetShader();
        void HSSetSamplers();
        void HSSetConstantBuffers();
        void DSSetShaderResources();
        void DSSetShader();
        void DSSetSamplers();
        void DSSetConstantBuffers();
        void CSSetUnorderedAccessViews1();
        void CSSetShader1();
        void CSSetSamplers1();
        void CSSetConstantBuffers1();
        [PreserveSig]
        int Map(IntPtr pResource, uint Subresource, int MapType, uint MapFlags, out D3D11_MAPPED_SUBRESOURCE pMappedResource);
        [PreserveSig]
        void Unmap(IntPtr pResource, uint Subresource);
    }
}
