using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LtcPlayer.App;
namespace LtcPlayer.Avalonia;
public partial class MainWindow
{
 readonly CoverController cover=new();
 readonly Dictionary<string,MediaDecoder> playbackAnalyses=new(StringComparer.OrdinalIgnoreCase);
 readonly SemaphoreSlim syncGate=new(1);
 readonly DispatcherTimer coverTimer=new(){Interval=TimeSpan.FromMilliseconds(16)};
 CancellationTokenSource? syncCancel;Task? syncPlayback;ProgramObservation? syncObservation;
 int programRevision,syncRevision;bool presentationReady;double retrySyncAt;string coverLoadWarning="";
 bool SyncOwnsSurface=>syncCancel!=null;
 internal bool LtcHasPriority=>monitor.Clock.Read().State is SyncState.LOCKED or SyncState.FREEWHEEL or SyncState.RESYNC;
 internal ProgramObservation? PreviewObservation=>syncObservation;
 internal CoverController Cover=>cover;
 void InitializePresentation(){
  Surface.Cover=cover;
  Editor.Changed+=()=>programRevision++;
  coverTimer.Tick+=(_,_)=>{if(!armed&&!SyncOwnsSurface&&presentationReady&&!closing)try{Surface.RefreshCover();}catch(Exception e){StatusMessage(e.Message);}UpdateCoverLabel();};
  Control<NumericUpDown>("FadeSeconds").ValueChanged+=(_,_)=>{if(!presentationReady)return;var value=Control<NumericUpDown>("FadeSeconds").Value;if(value is {} seconds){settings=settings with{FadeSeconds=(double)seconds};try{SaveSettings();}catch(Exception e){StatusMessage(e.Message);}}};
  coverTimer.Start();
 }
 async Task LoadPresentationSettingsAsync(){
  try{cover.Image=await CoverImageLoader.LoadAsync(settings.CoverPath);}catch(Exception e){cover.Image=null;coverLoadWarning=e.Message;StatusMessage("蓋画を読み込めません。黒を使用します："+e.Message);}
  Control<NumericUpDown>("FadeSeconds").Value=(decimal)settings.FadeSeconds;
 }
 internal void SetCoverVisible(bool value){cover.Set(value,settings.FadeSeconds);UpdateCoverLabel();}
 void CoverClick(object? sender,RoutedEventArgs e)=>SetCoverVisible(Control<ToggleButton>("CoverButton").IsChecked==true);
 void UpdateCoverLabel(){
  var button=Control<ToggleButton>("CoverButton");button.IsChecked=cover.IsCovered;
  Control<TextBlock>("CoverStatus").Text=cover.Transitioning?(cover.IsCovered?"蓋画へフェード中":"映像へフェード中"):cover.IsCovered?(cover.Image==null?"蓋画表示中（黒）":"蓋画表示中"):"映像表示中";
  ToolTip.SetTip(button,cover.Image==null?(coverLoadWarning!=""?"画像を読み込めないため黒を使用します："+coverLoadWarning:"未登録のため黒へ切り替えます"):cover.Image.Path);
 }
 async Task UpdateAutoPreviewAsync(){
  if(!presentationReady||closing||closePending||armed||busy||Preview.IsBusy||!Surface.Ready||MasterClock.Timestamp()<retrySyncAt)return;
  if(!await syncGate.WaitAsync(0))return;
  try{
   if(armed||busy||closing)return;
   if(SyncOwnsSurface&&(syncRevision!=programRevision||!Editor.CanArm||syncPlayback?.IsCompleted==true))await StopSyncCoreAsync();
   if(!SyncOwnsSurface&&LtcHasPriority&&Editor.CanArm){
    var cues=ShowProgramService.Validate(Editor.Rows,Editor.Rate);if(cues.Count==0)return;
    syncCancel=new();var token=syncCancel.Token;syncRevision=programRevision;
    await Preview.SetArmedAsync(true);
    if(armed||busy||closing){await StopSyncCoreAsync();return;}
    var display=selectedDisplay??new OutputDisplay{Width=16,Height=9};double scale=640d/Math.Max(display.Width,display.Height);
    display.Width=Math.Max(4,(int)(display.Width*scale/4)*4);display.Height=Math.Max(4,(int)(display.Height*scale/4)*4);
    var presentation=new PreviewProgramPresentation(Surface,display);
    syncPlayback=RunSyncPreviewAsync(cues,display,presentation,token);UpdateEnabled();
   }
  }catch(Exception e){retrySyncAt=MasterClock.Timestamp()+2;StatusMessage("同期プレビュー："+e.Message);await StopSyncCoreAsync();}
  finally{syncGate.Release();}
 }
 async Task RunSyncPreviewAsync(List<ScheduledCue> cues,OutputDisplay display,IProgramPresentation presentation,CancellationToken token){
  try{await Task.Run(()=>new ProgramPlayback(new UiDispatcher()).RunAsync(cues,display,monitor.Clock,settings.Offset/Editor.Rate.Fps,t=>syncObservation=t,token,presentation:presentation,cover:cover,analysisCache:playbackAnalyses));}
  catch(OperationCanceledException)when(token.IsCancellationRequested){}
  catch(Exception e){retrySyncAt=MasterClock.Timestamp()+2;StatusMessage("同期プレビュー："+e.Message);}
 }
 async Task StopSyncCoreAsync(){
  syncCancel?.Cancel();if(syncPlayback!=null)await syncPlayback;syncPlayback=null;syncCancel?.Dispose();syncCancel=null;syncObservation=null;
  if(!armed){await Preview.SetArmedAsync(false);Control<TextBlock>("RunInfo").Text="";}UpdateEnabled();
 }
 async Task RestorePreviewAsync(ProgramObservation snapshot){
  var row=Editor.Rows.FirstOrDefault(r=>r.CueId==snapshot.CueId);if(row==null||LtcHasPriority)return;
  previewCue=row.CueId;rebinding=true;try{Grid.SelectedItem=row;}finally{rebinding=false;}
  try{await Preview.LoadAsync(row.FilePath);if(LtcHasPriority)return;await Preview.SeekAsync(snapshot.FramePts??0);if(!LtcHasPriority&&snapshot.Manual&&!snapshot.Holding)await Preview.PlayAsync();}
  catch(Exception e){StatusMessage("プレビューへの引き継ぎ："+e.Message);}
 }
 internal async Task StopSyncPreviewAsync(){await syncGate.WaitAsync();try{await StopSyncCoreAsync();}finally{syncGate.Release();}}
}
