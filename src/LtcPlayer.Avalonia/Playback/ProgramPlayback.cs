using Dispatcher = LtcPlayer.Avalonia.UiDispatcher;
namespace LtcPlayer.App;
public record ProgramObservation(SyncState State,string Ltc,Guid? CueId,Guid? PreloadId,double Target,double? FramePts,int Presented,int Skipped,int Repeated,int Seeks,string Warning)
{
    public double HostSeconds{get;init;}=MasterClock.Timestamp();
    public uint VerificationPixelBgra{get;init;}
    public ProgramMonitorStatus? Monitor{get;init;}
}
public sealed class ProgramPlayback
{
    internal static Action<VideoRenderer>? ProbeObserver {get;set;}
    private readonly Dispatcher dispatcher;
    public ProgramPlayback(Dispatcher dispatcher)=>this.dispatcher=dispatcher;
    private sealed class CueFrames : IAsyncDisposable
    {
        public ScheduledCue Cue{get;}
        private readonly FrameBuffer buffer;
        private VideoFrame? current,next;
        public CueFrames(ScheduledCue cue,OutputDisplay display,double seek,CancellationToken token){Cue=cue;buffer=new(cue.FilePath,display.Width,display.Height,seek,token);}
        public void Prime(){if(next==null)buffer.TryRead(out next);}
        public VideoFrame? Select(double seconds,out int skipped)
        {
            int advanced=0;Prime();
            while(next!=null && next.Seconds<=seconds+0.000001){current=next;next=null;++advanced;Prime();}
            skipped=Math.Max(0,advanced-1);return current;
        }
        public ValueTask DisposeAsync()=>buffer.DisposeAsync();
    }
    public async Task RunAsync(IReadOnlyList<ScheduledCue> cues,OutputDisplay display,MasterClock clock,double offset,Action<ProgramObservation> observe,CancellationToken token,bool verify=false,nint monitorWindow=0,Action? stopRequested=null,Action<int>? monitorError=null)
    {
        var scheduler=new CueScheduler(cues);VideoRenderer? renderer=null;CueFrames? active=null,preload=null;bool monitorFaultNotified=false;
        var black=new byte[checked(display.Width*display.Height*4)];int epoch=-1,presented=0,skipped=0,repeated=0,seeks=0;
        double? lastPts=null;Guid? lastId=null,displayedId=null;double? displayedPts=null;uint verificationPixel=0;
        ProgramMonitorStatus? monitorStatus=null;double nextMonitorSample=0;
        try
        {
            await dispatcher.InvokeAsync(()=>{renderer=new VideoRenderer(display);if(monitorWindow!=0){renderer.EnableStopConfirmation();renderer.AttachMonitor(monitorWindow);}});
            while(!token.IsCancellationRequested)
            {
                // A held output still needs monitor presentation (e.g. after occlusion).
                // Native throttling keeps this at 15 fps without decoding another frame.
                if(monitorWindow!=0)await dispatcher.InvokeAsync(()=>{
                    if(renderer!.PollStopRequest())stopRequested?.Invoke();renderer.UpdateMonitor();
                    ProbeObserver?.Invoke(renderer);
                    double stamp=MasterClock.Timestamp();if(stamp>=nextMonitorSample){monitorStatus=renderer.MonitorStatus();nextMonitorSample=stamp+1;}
                    int error=monitorStatus?.Error??0;if(error!=0&&!monitorFaultNotified){monitorFaultNotified=true;monitorError?.Invoke(error);}
                });
                var s=clock.Read();double now=s.Seconds+offset;
                string warning="";byte[]? pixels=null;VideoFrame? texture=null;
                var selected=s.State==SyncState.WAITING?null:scheduler.Select(now);
                var upcoming=scheduler.Next(s.State==SyncState.WAITING?double.NegativeInfinity:now);
                if(s.State is not (SyncState.LOST or SyncState.ERROR))
                {
                    bool jump=epoch!=s.Generation;
                    if(jump){if(active!=null){await active.DisposeAsync();active=null;}if(preload!=null){await preload.DisposeAsync();preload=null;}epoch=s.Generation;}
                    if(active?.Cue.CueId!=selected?.CueId)
                    {
                        if(active!=null){await active.DisposeAsync();active=null;}
                        if(selected!=null)
                        {
                            double relative=now-selected.Start;
                            if(preload?.Cue.CueId==selected.CueId && relative<0.5){active=preload;preload=null;}
                            else {active=new(selected,display,Math.Max(0,relative-0.2),token);++seeks;}
                        }
                    }
                    if(upcoming?.CueId!=preload?.Cue.CueId)
                    {
                        if(preload!=null){await preload.DisposeAsync();preload=null;}
                        if(upcoming!=null)preload=new(upcoming,display,0,token);
                    }
                    preload?.Prime();
                    if(selected==null)pixels=black;
                    else
                    {
                        var frame=active!.Select(now-selected.Start,out int dropped);skipped+=dropped;
                        if(frame==null)warning="準備待ち: 前の出力を保持しています。";
                        else {pixels=frame.Pixels;texture=frame;displayedId=selected.CueId;displayedPts=frame.Seconds;if(lastId==selected.CueId && lastPts==frame.Seconds)++repeated;lastId=selected.CueId;lastPts=frame.Seconds;}
                    }
                    if(pixels==black){displayedId=null;displayedPts=null;}
                }
                if(pixels!=null)
                {
                    byte[]? verifiedPixels=null;
                    await dispatcher.InvokeAsync(()=>{token.ThrowIfCancellationRequested();if(texture!=null)renderer!.Present(texture);else renderer!.Present(pixels);if(verify){if(texture?.TextureFormat is >0)verifiedPixels=renderer!.Readback();else{renderer!.Verify(pixels);verifiedPixels=pixels;}}});++presented;
                    if(verify)verificationPixel=BitConverter.ToUInt32(verifiedPixels!,(verifiedPixels!.Length/8)*4);
                }
                observe(new(s.State,s.Label,displayedId,preload?.Cue.CueId,now,displayedPts,presented,skipped,repeated,seeks,warning){VerificationPixelBgra=verificationPixel,Monitor=monitorStatus});
                await Task.Delay(8,token).ConfigureAwait(false);
            }
        }
        finally{if(active!=null)await active.DisposeAsync();if(preload!=null)await preload.DisposeAsync();if(renderer!=null)await dispatcher.InvokeAsync(renderer.Dispose);}
    }
}

