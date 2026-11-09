using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using LtcPlayer.App;
namespace LtcPlayer.Avalonia;
public static class PreviewRecoveryVerification
{
 [DllImport("user32.dll")]static extern bool ShowWindow(nint window,int command);
 static void Check(bool ok,string message){if(!ok)throw new IOException(message);}
 public static async Task Fresh(VideoRenderer r,ulong before){for(int i=0;i<150;i++){r.UpdateMonitor();var s=r.MonitorStatus();if(s.Error!=0)throw new IOException($"monitor 0x{s.Error:X8}");if(s.Rendered>before&&s.Presented==s.Rendered)return;await Task.Delay(15);}throw new TimeoutException("fresh mirror presentation timeout: "+JsonSerializer.Serialize(r.MonitorStatus()));}
 public static async Task RunAsync(MainWindow w,string folder)
 {
  Directory.CreateDirectory(folder);var results=new List<object>();bool failed=false;
  async Task Test(string name,Func<Task<object>> body){try{results.Add(new{test=name,status="PASS",detail=await body()});}catch(Exception e){failed=true;results.Add(new{test=name,status="FAIL",detail=e.ToString()});}File.WriteAllText(Path.Combine(folder,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));}
  void Stage(string name)=>File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{Stage=name,Observation=w.LatestObservation}));
  try{
   await Test("BGRA mirror colors / held output / hidden and restored child",async()=>{
    var target=w.Surface.BeginProgram();try{
     using var r=new VideoRenderer(VideoRenderer.Displays().First(d=>d.Primary!=0));r.AttachMonitor(target);var d=VideoRenderer.Displays().First(d=>d.Primary!=0);var statuses=new List<object>();
     foreach(var color in new[]{(B:0,G:0,R:255),(B:0,G:255,R:0),(B:255,G:0,R:0),(B:0,G:0,R:0)}){
      var bytes=new byte[d.Width*d.Height*4];for(int i=0;i<bytes.Length;i+=4){bytes[i]=(byte)color.B;bytes[i+1]=(byte)color.G;bytes[i+2]=(byte)color.R;bytes[i+3]=255;}
      ulong before=r.MonitorStatus().Rendered;r.Present(bytes);await Fresh(r,before);var mirror=r.MonitorReadback();var size=r.MonitorStatus();
      int nonmatch=0;for(int y=1;y<size.Height-1;y++)for(int x=1;x<size.Width-1;x++){int p=(y*size.Width+x)*4;if(mirror[p]!=color.B||mirror[p+1]!=color.G||mirror[p+2]!=color.R)nonmatch++;}
      Check(nonmatch==0,"solid color mismatch: "+nonmatch);statuses.Add(new{color,size});
     }
     var green=new byte[d.Width*d.Height*4];for(int i=0;i<green.Length;i+=4){green[i+1]=255;green[i+3]=255;}ulong start=r.MonitorStatus().Rendered;r.Present(green);await Fresh(r,start);
     var held=r.Readback();ulong frames=r.MonitorStatus().Presented;for(int i=0;i<35;i++){r.UpdateMonitor();await Task.Delay(15);}Check(r.MonitorStatus().Presented>frames,"held monitor stopped");Check(held.SequenceEqual(r.Readback()),"production changed while held");
     ShowWindow(target,0);try{for(int i=0;i<25;i++){r.UpdateMonitor();await Task.Delay(15);}}finally{ShowWindow(target,5);}
     start=r.MonitorStatus().Rendered;await Fresh(r,start);var restored=r.MonitorReadback();var s=r.MonitorStatus();int center=(s.Height/2*s.Width+s.Width/2)*4;Check(restored[center+1]==255&&restored[center]==0,"restored mirror black");Check(held.SequenceEqual(r.Readback()),"restore changed production");
     RegressionWindow.SavePng(Path.Combine(folder,"restored-green.png"),restored,s.Width,s.Height);return new{Colors=statuses,HeldAndRestored=s};
    }finally{w.Surface.EndProgram();}
   });
   await Test("HAP production / LTC loss / monitor restore / input resume / stop",async()=>{
    var d=VideoRenderer.Displays().First(x=>x.Primary!=0);await w.ApplySettingsAsync(new(new(Rate:"29.97 DF",Freewheel:.2),d));
    var cue=new CueRow{FilePath=Program.Option("--media")??throw new IOException("--media missing")};cue.SetStart("00:00:00:00");await ShowProgramService.RefreshAsync(cue,CancellationToken.None);w.Editor.Add(cue);
    int receiving=1,index=60;await w.ArmAsync(async(clock,token)=>{while(!token.IsCancellationRequested){if(Volatile.Read(ref receiving)!=0){int f=60+Interlocked.Increment(ref index)%180;clock.Accept(new TcFrame{Seconds=f/30,Frame=f%30,Drop=1},MasterClock.Timestamp());}await Task.Delay(33,token);}});
    async Task Until(Func<bool> test){for(int i=0;i<400;i++){if(test())return;await Task.Delay(15);}throw new TimeoutException("production state: "+JsonSerializer.Serialize(w.LatestObservation));}
    try{
     await Until(()=>w.LatestObservation is{FramePts:>0,Monitor.Presented:>2});Stage("streaming");await Task.Delay(600);
     Volatile.Write(ref receiving,0);await Until(()=>w.LatestObservation?.State==SyncState.LOST);var before=w.LatestObservation!;ulong count=before.Monitor!.Presented;int production=before.Presented;Stage("held-visible");
     await Task.Delay(Program.Arguments.Contains("--visual-hold")?15000:1600);
     var after=w.LatestObservation!;Check(after.Presented==production,"LOST unexpectedly presented production");Check(after.Monitor!.Presented>count,"LOST mirror stopped");
     var captured=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
     ProgramPlayback.ProbeObserver=r=>{ProgramPlayback.ProbeObserver=null;try{var s=r.MonitorStatus();var src=r.Readback();var dst=r.MonitorReadback();RegressionWindow.SavePng(Path.Combine(folder,"production-held.png"),src,d.Width,d.Height);RegressionWindow.SavePng(Path.Combine(folder,"mirror-held.png"),dst,s.Width,s.Height);Check(dst.Where((b,i)=>i%4!=3).Any(b=>b>50),"held GPU mirror is black");captured.SetResult();}catch(Exception e){captured.SetException(e);}};
     await captured.Task.WaitAsync(TimeSpan.FromSeconds(3));
     // Only the verification child is hidden; the production output is untouched.
     var target=w.Surface.ProgramWindowForVerification;ShowWindow(target,0);await Task.Delay(350);ShowWindow(target,5);await Task.Delay(750);Stage("restored-held");
     if(Program.Arguments.Contains("--visual-hold"))await Task.Delay(15000);
     Check(w.LatestObservation!.Monitor!.Error==0,"monitor restore failed");
     Volatile.Write(ref receiving,1);await Until(()=>w.LatestObservation!.Presented>production+5&&w.LatestObservation.State!=SyncState.LOST);Stage("resumed");await Task.Delay(500);
     var resumed=w.LatestObservation;w.ConfirmStopForTest=_=>Task.FromResult(true);await w.RequestStopAsync(false);Check(!w.IsArmed&&!w.Surface.ProgramMode,"stop did not restore cue preview");return new{Before=before,AfterHold=after,Resumed=resumed,Stopped=true};
    }finally{ProgramPlayback.ProbeObserver=null;w.ConfirmStopForTest=null;await w.DisarmAsync();}
   });
  }finally{await w.Monitor.DisposeAsync();Stage("complete");File.WriteAllText(Path.Combine(folder,"exit-code.txt"),failed?"1":"0");MainWindow.Shutdown(failed?1:0);}
 }
}
