using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using LtcPlayer.App;
using System.Runtime.InteropServices;
namespace LtcPlayer.Avalonia;
public sealed class PreviewHost : NativeControlHost
{
 nint renderer,window,container;
 public bool ProgramMode{get;private set;}
 internal nint ProgramWindowForVerification=>ProgramMode?window:throw new InvalidOperationException("Not in production mode");
 public nint BeginProgram(){Dispatcher.UIThread.VerifyAccess();if(ProgramMode)throw new InvalidOperationException("Already in production mode");if(renderer!=0){N.output_destroy(renderer);renderer=0;}N.preview_window_destroy(window);window=0;Check(N.preview_child_create(container,out window));ProgramMode=true;return window;}
 public void EndProgram(){Dispatcher.UIThread.VerifyAccess();if(!ProgramMode)return;N.preview_window_destroy(window);window=0;Check(N.preview_create(container,out renderer,out window));ProgramMode=false;Black();}
 public bool Ready=>renderer!=0;
 public int Presents{get;private set;}
 public (int Width,int Height) PixelSize {get{Dispatcher.UIThread.VerifyAccess();Check(N.preview_size(renderer,out int w,out int h));return(w,h);}}
 protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
 {
  Check(N.preview_host_create(parent.Handle,out renderer,out container,out window));return new PlatformHandle(container,"HWND");
 }
 protected override void DestroyNativeControlCore(IPlatformHandle handle){if(renderer!=0){N.output_destroy(renderer);renderer=0;}N.preview_window_destroy(container);container=window=0;}
 public void Present(VideoFrame frame)
 {
  Dispatcher.UIThread.VerifyAccess();if(!Ready)throw new ObjectDisposedException(nameof(PreviewHost));
  _=PixelSize;
  Check(N.output_hap(renderer,frame.Pixels,frame.Pixels.Length,frame.TextureFormat==0?640:frame.Width,frame.TextureFormat==0?360:frame.Height,frame.TextureFormat==0?3:frame.TextureFormat));Presents++;
 }
 public void Black(){if(Ready)Present(new(new byte[640*360*4],0));}
 public byte[] Readback(){var s=PixelSize;var b=new byte[s.Width*s.Height*4];Check(N.output_readback(renderer,b,b.Length));return b;}
 static void Check(int hr){if(hr!=0)throw new IOException($"D3D11 preview failure 0x{hr:X8}");}
 static class N
 {
  const string D="PlayerNative.dll";
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int preview_child_create(nint parent,out nint window);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int preview_host_create(nint parent,out nint renderer,out nint container,out nint window);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int preview_resume(nint window,out nint renderer);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern void preview_window_destroy(nint window);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int preview_create(nint parent,out nint renderer,out nint window);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int preview_size(nint handle,out int w,out int h);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int output_hap(nint h,byte[] data,int length,int w,int hgt,int kind);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern int output_readback(nint h,[Out]byte[] data,int length);
  [DllImport(D,CallingConvention=CallingConvention.Cdecl)]internal static extern void output_destroy(nint h);
 }
}
