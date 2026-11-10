namespace LtcPlayer.App;
public sealed class PreviewProgramPresentation : IProgramPresentation
{
 readonly global::LtcPlayer.Avalonia.PreviewHost host;readonly OutputDisplay display;
 public PreviewProgramPresentation(global::LtcPlayer.Avalonia.PreviewHost host,OutputDisplay display){this.host=host;this.display=display;}
 public void Present(VideoFrame frame)=>host.Present(frame.TextureFormat==0?frame with{TextureFormat=3,Width=display.Width,Height=display.Height}:frame);
 public void Refresh()=>host.RefreshCover();
 public void Dispose(){}
}
