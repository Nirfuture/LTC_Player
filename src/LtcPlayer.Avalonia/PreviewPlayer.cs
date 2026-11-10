using System.Diagnostics;
using LtcPlayer.App;
namespace LtcPlayer.Avalonia;
public enum PreviewState {Empty,Ready,Playing,Paused,Stopped,Ended,Error}
public record PreviewSnapshot(PreviewState State,double Position,double Duration,double? FramePts,string Name,string Notice,int Presented,AudioProgress? Audio);
public sealed class PreviewPlayer : IAsyncDisposable
{
 readonly PreviewHost host;readonly SemaphoreSlim commands=new(1);
 readonly Stopwatch clock=new();
 readonly MediaDecoder decoder=new(reuseAnalysis:true);
 double? stillPosition;
 MediaInfo? media;PreviewAudioInfo? audioInfo;string path="",errorNotice="";double origin,position;int presented;
 CancellationTokenSource? cancel;Task? loop;PreviewAudio? audio;
 PreviewState state;bool armed;float volume=.5f;bool muted;
 public PreviewSnapshot Snapshot=>new(state,position,media?.Duration??0,last?.Seconds,Path.GetFileName(path),errorNotice!=""?errorNotice:audioInfo?.Notice??"",presented,audio?.Progress);
 VideoFrame? last;
 public bool Armed=>armed;
 public string LoadedPath=>path;
 public double FrameRate=>media?.FrameRate??0;
 public event Action<PreviewSnapshot>? Changed;
 public PreviewPlayer(PreviewHost host)=>this.host=host;
 public void SetVolume(double value,bool mute){volume=(float)Math.Clamp(value,0,1);muted=mute;if(audio!=null)audio.Gain=muted?0:volume;}
 public async Task LoadAsync(string file)
 {
  await commands.WaitAsync();try{
   RejectArmed();await HaltAsync();media=null;audioInfo=null;path="";errorNotice="";state=PreviewState.Empty;last=null;host.Black();Notify();
   stillPosition=null;
   var videoTask=Task.Run(()=>decoder.InspectAsync(file,CancellationToken.None));
   var audioTask=Task.Run(()=>PreviewAudio.InspectAsync(file,CancellationToken.None));
   await Task.WhenAll(videoTask,audioTask);
   var info=await videoTask;var sound=await audioTask;
   media=info;audioInfo=sound;path=file;position=0;state=PreviewState.Ready;
   await StillAsync(0);Notify();
  }finally{commands.Release();}
 }
 public async Task PlayAsync()
 {
  await commands.WaitAsync();try{
   RejectArmed();if(media==null)throw new InvalidOperationException("プレビューするキューを選んでください。");
   if(state==PreviewState.Playing)return;
   if(state==PreviewState.Paused&&loop!=null){if(audio!=null)await audio.SetPausedAsync(false,CancellationToken.None);clock.Start();state=PreviewState.Playing;Notify();return;}
   await HaltAsync();errorNotice="";if(position>=media.Duration-.001)position=0;
   cancel=new();origin=position;clock.Reset();
   if(audioInfo!.Supported){audio=new(path,position,media.Duration,muted?0:volume);try{await audio.ReadyAsync(cancel.Token);}catch{await HaltAsync();throw;}}
   // Decode and show an initial frame before the preview audio clock starts.
   if(last==null||stillPosition!=position)await StillAsync(position);
   stillPosition=null;state=PreviewState.Playing;clock.Start();if(audio!=null)await audio.SetPausedAsync(false,cancel.Token);
   loop=RunAsync(cancel.Token);Notify();
  }finally{commands.Release();}
 }
 public async Task PauseAsync()
 {
  await commands.WaitAsync();try{if(state!=PreviewState.Playing)return;state=PreviewState.Paused;clock.Stop();if(audio!=null)await audio.SetPausedAsync(true,CancellationToken.None);position=CurrentPosition();Notify();}finally{commands.Release();}
 }
 public async Task StopAsync()
 {
  await commands.WaitAsync();try{await HaltAsync();position=0;state=media==null?PreviewState.Empty:PreviewState.Stopped;host.Black();last=null;Notify();}finally{commands.Release();}
 }
 public async Task SeekAsync(double seconds)
 {
  bool resume=false;
  await commands.WaitAsync();try{
   RejectArmed();if(media==null)return;resume=state==PreviewState.Playing;
   await HaltAsync();position=Math.Clamp(seconds,0,Math.Max(0,media.Duration-.001));state=PreviewState.Paused;
   await StillAsync(position);Notify();
  }finally{commands.Release();}
  if(resume)await PlayAsync();
 }
 public async Task SetArmedAsync(bool value)
 {
  await commands.WaitAsync();try{armed=value;if(value){await HaltAsync();position=0;state=media==null?PreviewState.Empty:PreviewState.Stopped;host.Black();last=null;}Notify();}finally{commands.Release();}
 }
 void RejectArmed(){if(armed)throw new InvalidOperationException("本番ARM中はプレビューを再生・変更できません。");}
 double CurrentPosition()=>Math.Min(media!.Duration,origin+(audio?.Progress.Seconds??clock.Elapsed.TotalSeconds));
 async Task StillAsync(double at)
 {
  var frame=await Task.Run(async()=>{await foreach(var f in decoder.DecodeAsync(path,640,360,CancellationToken.None,at))return f;return null;});
  stillPosition=frame!=null?at:null;
  if(frame!=null){host.Present(frame);last=frame;presented++;}else host.Black();
 }
 async Task RunAsync(CancellationToken token)
 {
  await using var buffer=new FrameBuffer(path,640,360,origin,token,decoder);VideoFrame? next=null,current=last;
  try {
   while(!token.IsCancellationRequested){
    if(state==PreviewState.Playing){
     if(audio?.Error is {} e)throw e;
     position=CurrentPosition();if(audio?.Progress.Done==true&&media!.Duration-position<.1)position=media.Duration;
     if(next==null)buffer.TryRead(out next);
     while(next!=null&&next.Seconds<=position+.000001){current=next;next=null;buffer.TryRead(out next);}
     if(current!=null){host.Present(current);last=current;presented++;}
     if(position>=media!.Duration){state=PreviewState.Ended;clock.Stop();Notify();return;}
     Notify();
    }
    await Task.Delay(12,token);
   }
  }catch(OperationCanceledException)when(token.IsCancellationRequested){}
  catch(Exception e){state=PreviewState.Error;errorNotice=e.Message;clock.Stop();if(audio!=null){await audio.DisposeAsync();audio=null;}Notify();}
 }
 async Task HaltAsync()
 {
  cancel?.Cancel();if(loop!=null){await loop;loop=null;}if(audio!=null){await audio.DisposeAsync();audio=null;}
  cancel?.Dispose();cancel=null;clock.Reset();
 }
 void Notify()=>Changed?.Invoke(Snapshot);
 public async ValueTask DisposeAsync(){await commands.WaitAsync();try{await HaltAsync();}finally{commands.Release();}}
}
