using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LtcPlayer.App;
using System.ComponentModel;
namespace LtcPlayer.Avalonia;
public enum DeliveryState{Stopped,Starting,Streaming,ConfirmingStop,Stopping}
public partial class MainWindow
{
 readonly LtcMonitorService monitor=new();OutputDisplay? selectedDisplay;string? selectedDisplayName;
 void SaveSettings()
 {
  string file=Program.Option("--settings")??InputSettings.PathName;var node=System.Text.Json.JsonSerializer.SerializeToNode(settings)!.AsObject();node["OutputDisplayName"]=selectedDisplayName;
  File.WriteAllText(file+".tmp",node.ToJsonString(new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));File.Move(file+".tmp",file,true);
 }
 void LoadOutputSelection()
 {
  string file=Program.Option("--settings")??InputSettings.PathName;
  if(File.Exists(file)){using var doc=System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));if(doc.RootElement.TryGetProperty("OutputDisplayName",out var name))selectedDisplayName=name.GetString();}
  var displays=VideoRenderer.Displays();selectedDisplay=selectedDisplayName==null?displays.FirstOrDefault():displays.Where(d=>d.Name==selectedDisplayName).Select(d=>(OutputDisplay?)d).FirstOrDefault();
 }
 ManualOutputControl manualOutput=new();
 internal bool CanManualOutput=>delivery==DeliveryState.Streaming&&monitor.Clock.Read().State is SyncState.WAITING or SyncState.LOST;
 internal void PlayManual(CueRow row){if(!CanManualOutput||!row.Enabled)return;manualOutput.Play(row.CueId);}
 internal void StopManual(){if(CanManualOutput)manualOutput.Stop();}
 DeliveryState delivery;bool keyboardSeeking,closePending;Guid? previewCue;int surfaceGeneration;
 Task<bool>? stopRequest;
 internal Func<bool,Task<bool>>? ConfirmStopForTest {get;set;}
 internal DeliveryState Delivery=>delivery;
 internal LtcMonitorService Monitor=>monitor;
 void CancelScrub(){scrubbing=false;keyboardSeeking=false;surfaceGeneration++;}
 void TickStatus()
 {
  var clock=monitor.Clock.Read();Control<TextBlock>("LtcLabel").Text=clock.Label;
  Control<TextBlock>("LtcLabel").Opacity=clock.State is SyncState.LOST or SyncState.ERROR?.45:1;
  if(armed&&observation is {} t){var cue=Editor.Rows.FirstOrDefault(c=>c.CueId==t.CueId);Control<TextBlock>("PreviewName").Text=CueName(t.CueId);Control<TextBlock>("PreviewTime").Text=MediaTimeDisplay.Format(t.FramePts,cue?.FrameRate??0);Control<TextBlock>("RunInfo").Text=$"{(t.Manual?(t.Holding?"手動出力・保持":"手動出力"):"LTC同期")} {CueName(t.CueId)}\n次の動画 {CueName(t.PreloadId)}\nスキップフレーム数 {t.Skipped}\n{t.Warning}";}
  UpdateRowControls();UpdatePreviewGeometry();
 }
 void UpdateDeliveryButton()
 {
  var button=Control<Button>("ArmButton");button.Content=delivery switch{DeliveryState.Starting=>"出力準備中",DeliveryState.Stopping=>"停止中",DeliveryState.Streaming or DeliveryState.ConfirmingStop=>"出力停止",_=>"出力開始"};
  button.Background=new SolidColorBrush(Color.Parse(delivery is DeliveryState.Streaming or DeliveryState.ConfirmingStop?"#BC303A":"#303844"));
  string reason=DeliveryBlockReason();
  button.IsEnabled=!busy&&!closing&&(delivery is DeliveryState.Streaming || delivery==DeliveryState.Stopped&&reason=="");
  ToolTip.SetTip(button,reason==""?(delivery==DeliveryState.Streaming?"出力を停止します（確認あり）":"LTCに同期して出力を開始します"):reason);
  var hint=Control<TextBlock>("DeliveryHint");hint.IsVisible=delivery==DeliveryState.Stopped&&reason!="";hint.Text=hint.IsVisible?"出力できません："+reason:"";
  Control<TextBlock>("ModeLabel").Text=delivery==DeliveryState.Starting?"出力準備中":armed?"出力中":"プレビュー";
  if(armed)Control<TextBlock>("PreviewInfo").Text="本番映像モニター（無音）";
  Control<Button>("SettingsButton").IsEnabled=delivery is DeliveryState.Stopped or DeliveryState.Streaming;
  UpdateActivity();UpdatePreviewGeometry();
 }
 internal string DeliveryBlockReason()
 {
  if(closing)return "終了処理中です。";
  if(busy)return "編集・読込処理中です。";
  if(delivery!=DeliveryState.Stopped)return "";
  if(!Editor.Rows.Any(r=>r.Enabled))return "有効なキューを1件以上登録してください。";
  if(!Editor.CanArm)return Editor.Rows.Any(r=>r.Enabled&&r.Error=="同一開始TCの競合")?"開始TCが重複しています。時刻を変更するか、不要なキューの「有効」を外してください。":Editor.ValidationMessage;
  if(selectedDisplay is not {} display||!VideoRenderer.Displays().Any(d=>d.Name==display.Name&&d.Width==display.Width&&d.Height==display.Height))return "設定から接続済みの映像出力を選択してください。";
  return "";
 }
 void UpdateRowControls()
 {
  foreach(var rowControl in Grid.GetVisualDescendants().OfType<DataGridRow>()){
   bool live=armed&&rowControl.DataContext is CueRow cue&&cue.CueId==observation?.CueId;
   rowControl.Background=live?new SolidColorBrush(Color.Parse("#205C43")):Brushes.Transparent;
   foreach(string key in new[]{"DataGridRowSelectedBackgroundBrush","DataGridRowSelectedHoveredBackgroundBrush","DataGridRowSelectedUnfocusedBackgroundBrush","DataGridRowSelectedHoveredUnfocusedBackgroundBrush"}){
    if(live)rowControl.Resources[key]=rowControl.Background!;else rowControl.Resources.Remove(key);
   }
   ToolTip.SetTip(rowControl,live?"現在出力中のキュー":null);
  }
  foreach(var b in Grid.GetVisualDescendants().OfType<Button>())if(b.DataContext is CueRow row){
   bool current=previewCue==row.CueId;
   if(b.Classes.Contains("cue-play")){b.IsEnabled=(!armed||CanManualOutput&&row.Enabled)&&!busy&&!closing&&!Preview.IsBusy;bool pause=!armed&&current&&Preview.Snapshot.State==PreviewState.Playing;if(b.Tag is not bool wasPause||wasPause!=pause){b.Tag=pause;b.Content=new PathIcon{Width=12,Height=12,Data=Geometry.Parse(pause?"M 0,0 L 4,0 L 4,12 L 0,12 Z M 8,0 L 12,0 L 12,12 L 8,12 Z":"M 1,0 L 11,6 L 1,12 Z")};}ToolTip.SetTip(b,armed?"このキューを先頭から出力":pause?"一時停止":current&&Preview.Snapshot.State==PreviewState.Paused?"再開":"再生");}
   if(b.Classes.Contains("cue-stop"))b.IsEnabled=!busy&&!closing&&(armed?CanManualOutput&&observation is {Manual:true} t&&t.CueId==row.CueId:current);
  }
 }
 async void RowPlayClick(object? sender,RoutedEventArgs e)
 {
  if(sender is not Button{DataContext:CueRow row})return;
  if(armed){PlayManual(row);return;}
  await Safe(async()=>{bool same=previewCue==row.CueId;rebinding=true;try{Grid.SelectedItem=row;}finally{rebinding=false;}
   if(!same){previewCue=row.CueId;CancelScrub();await Preview.LoadAsync(row.FilePath);}
   if(same&&Preview.Snapshot.State==PreviewState.Playing)await Preview.PauseAsync();else await Preview.PlayAsync();UpdateRowControls();});
 }
 async void RowStopClick(object? sender,RoutedEventArgs e){if(armed){StopManual();return;}if(!armed&&sender is Button{DataContext:CueRow row}&&previewCue==row.CueId){CancelScrub();await Safe(()=>Preview.StopAsync());}}
 async void SettingsClick(object? sender,RoutedEventArgs e)
 {
  await Safe(async()=>{
   var result=await new SettingsDialog(settings,selectedDisplay,armed).ShowDialog<SettingsResult?>(this);
   if(result==null||armed)return;await ApplySettingsAsync(result);
  });
 }
 internal async Task ApplySettingsAsync(SettingsResult result)
 {
  if(armed)throw new InvalidOperationException("出力中は設定を変更できません。");
  OutputPresentation.Validate(result.Settings.OutputMode);
  var mode=TimecodeRate.Modes.First(r=>r.Name==result.Settings.Rate);_=new MasterClock(mode,result.Settings.Freewheel,result.Settings.Resync);
  if(!VideoRenderer.Displays().Any(d=>d.Name==result.Display.Name&&d.Width==result.Display.Width&&d.Height==result.Display.Height))throw new InvalidDataException("指定した出力画面が接続されていません。");
  var old=settings;var oldDisplay=selectedDisplay;var oldName=selectedDisplayName;
  busy=true;UpdateEnabled();try{CancelScrub();await Preview.StopAsync();await monitor.ConfigureAsync(result.Settings);settings=result.Settings;selectedDisplay=result.Display;selectedDisplayName=result.Display.Name;SaveSettings();Editor.Rate=mode;Editor.Validate();StatusMessage("設定を適用しました。");}
  catch{settings=old;selectedDisplay=oldDisplay;selectedDisplayName=oldName;await monitor.ConfigureAsync(old);throw;}
  finally{busy=false;UpdateEnabled();}
 }
 async void DeliveryClick(object? sender,RoutedEventArgs e)=>await Safe(async()=>{if(delivery==DeliveryState.Stopped)await ArmAsync();else if(delivery==DeliveryState.Streaming)await RequestStopAsync(false);});
 internal Task<bool> RequestStopAsync(bool exit)
 {
  if(!armed)return Task.FromResult(true);if(stopRequest!=null)return stopRequest;
  var task=ConfirmAndStopAsync(exit);stopRequest=task;return ClearRequestAsync(task);
 }
 async Task<bool> ClearRequestAsync(Task<bool> task){try{return await task;}finally{if(ReferenceEquals(stopRequest,task))stopRequest=null;}}
 async Task<bool> ConfirmAndStopAsync(bool exit)
 {
  if(delivery==DeliveryState.Starting){await DisarmAsync();return true;}
  delivery=DeliveryState.ConfirmingStop;UpdateDeliveryButton();
  bool accepted=ConfirmStopForTest!=null?await ConfirmStopForTest(exit):await StopDialog.ShowAsync(this,exit);
  if(accepted)await DisarmAsync();else if(armed){delivery=DeliveryState.Streaming;UpdateDeliveryButton();}
  return accepted;
 }
 async void OnWindowClosing(object? sender,WindowClosingEventArgs e)
 {
  if(closeAllowed)return;e.Cancel=true;if(closing||closePending)return;closePending=true;
  try{if(armed&&!await RequestStopAsync(true))return;closing=true;UpdateEnabled();await DisarmAsync();await Preview.DisposeAsync();await monitor.DisposeAsync();timer.Stop();closeAllowed=true;Close();}
  catch(Exception ex){closing=false;StatusMessage(ex.Message);UpdateEnabled();}
  finally{closePending=false;}
 }
 internal async Task ArmAsync(Func<MasterClock,CancellationToken,Task>? testSource=null)
 {
  if(armed||busy)return;armed=true;delivery=DeliveryState.Starting;Editor.Locked=true;productionCancel=new();var token=productionCancel.Token;manualOutput=new();CancelScrub();
  var prepared=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);armPrepared=prepared;UpdateEnabled();
  try {
   await Preview.SetArmedAsync(true);token.ThrowIfCancellationRequested();Grid.CommitEdit();
   foreach(var row in Editor.Rows.Where(r=>r.Enabled))await ShowProgramService.RefreshAsync(row,token);
   var cues=ShowProgramService.Validate(Editor.Rows,Editor.Rate);if(cues.Count==0)throw new InvalidDataException("有効キューがありません。");
   if(selectedDisplay is not {} display)throw new InvalidDataException("設定から映像出力を選択してください。");
   if(testSource!=null)await monitor.ConfigureAsync(settings,testSource);
   token.ThrowIfCancellationRequested();nint target=Surface.BeginProgram();observation=null;delivery=DeliveryState.Streaming;UpdateEnabled();
   production=RunProductionAsync(cues,display,monitor.Clock,token,target);StatusMessage("出力中。LTCに追従します。");
  }catch{await FinishProductionAsync();throw;}
  finally{prepared.TrySetResult();if(ReferenceEquals(armPrepared,prepared))armPrepared=null;}
 }
 async Task RunProductionAsync(List<ScheduledCue> cues,OutputDisplay display,MasterClock clock,CancellationToken token,nint target)
 {
  string final="出力を停止しました。";
  try {
   using var log=new SyncRunLog();log.Write(new{kind="configuration",settings,display,hostSeconds=MasterClock.Timestamp()});
   await Task.Run(()=>new ProgramPlayback(new UiDispatcher()).RunAsync(cues,display,clock,settings.Offset/Editor.Rate.Fps,t=>{observation=t;log.Write(new{kind="programObservation",t});},token,false,Program.Arguments.Contains("--diagnostic-no-monitor")?0:target,
    ()=>Dispatcher.UIThread.Post(()=>_ = Safe(async()=>{await RequestStopAsync(false);})),
    error=>Dispatcher.UIThread.Post(()=>{Control<TextBlock>("PreviewInfo").Text="出力モニターのエラー: "+error;}),settings.OutputMode,manualOutput));
  }catch(OperationCanceledException){}catch(Exception e){final="出力エラー: "+e.Message;}
  finally{await FinishProductionAsync();StatusMessage(final);}
 }
 async Task FinishProductionAsync()
 {
  // ProgramPlayback disposes the borrowed monitor swap chain before this callback.
  Surface.EndProgram();await Preview.SetArmedAsync(false);armed=false;delivery=DeliveryState.Stopped;Editor.Locked=false;productionCancel?.Dispose();productionCancel=null;observation=null;Control<TextBlock>("RunInfo").Text="";UpdateEnabled();Editor.Validate();
  CancelScrub();UpdatePreview(Preview.Snapshot);if(!closing)await SelectPreviewAsync();
 }
 internal async Task DisarmAsync()
 {
  if(armed){delivery=DeliveryState.Stopping;UpdateDeliveryButton();}productionCancel?.Cancel();if(armPrepared is {} prepared)await prepared.Task;
  if(production is {} run){await run;production=null;}
 }
}
