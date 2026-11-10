using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LtcPlayer.App;
using System.Globalization;
namespace LtcPlayer.Avalonia;
public partial class MainWindow : Window
{
 internal readonly ShowEditor Editor=new();
 internal PreviewPlayer Preview=null!;
 internal PreviewHost Surface=>Control<PreviewHost>("PreviewSurface");
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(100)};
 InputSettings settings=new();bool busy,armed,closeAllowed,closing,updatingSeek,dragging,scrubbing,rebinding;long selectionVersion;
 CancellationTokenSource? productionCancel;Task? production;
 TaskCompletionSource? armPrepared;
 ProgramObservation? observation;
 CueRow? dragged;Point dragOrigin;
 internal bool IsArmed=>armed;
 internal ProgramObservation? LatestObservation=>observation;
 T Control<T>(string name)where T:Control=>this.FindControl<T>(name)!;
 DataGrid Grid=>Control<DataGrid>("CueGrid");
 
 void StatusMessage(string s)=>Control<TextBlock>("Status").Text=s;
 public MainWindow()
 {
  AvaloniaXamlLoader.Load(this);Grid.ItemsSource=Editor.Rows;
  AddHandler(PointerPressedEvent,(_,e)=>TracePointer("press",e),RoutingStrategies.Tunnel,true);
  AddHandler(PointerReleasedEvent,(_,e)=>TracePointer("release",e),RoutingStrategies.Tunnel,true);
  Preview=new(Surface);Preview.Changed+=UpdatePreview;
  Control<Grid>("PreviewViewport").SizeChanged+=(_,_)=>UpdatePreviewGeometry();
  Editor.Changed+=()=>{Control<TextBlock>("ProgramInfo").Text=Editor.ValidationMessage;UpdateEnabled();};
  Grid.SelectionChanged+=async(_,_)=>await SelectPreviewAsync();
  Control<Slider>("Volume").PropertyChanged+=(_,e)=>{if(e.Property.Name=="Value")ApplyVolume();};
  Control<CheckBox>("Mute").IsCheckedChanged+=(_,_)=>ApplyVolume();
  DragDrop.AddDragOverHandler(Grid,DragOver);DragDrop.AddDropHandler(Grid,Drop);
  Grid.AddHandler(PointerMovedEvent,HandleMoved,RoutingStrategies.Tunnel,true);
  Grid.AddHandler(PointerReleasedEvent,HandleReleased,RoutingStrategies.Tunnel,true);
  var seek=Control<Slider>("Seek");
  void Scrub(PointerEventArgs e){double x=e.GetPosition(seek).X;seek.Value=Math.Clamp((x-5)/Math.Max(1,seek.Bounds.Width-10),0,1)*seek.Maximum;}
  seek.AddHandler(KeyDownEvent,(_,e)=>{if(e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown)keyboardSeeking=true;},RoutingStrategies.Tunnel);
  seek.PointerCaptureLost+=(_,_)=>{scrubbing=false;};
  seek.LostFocus+=(_,_)=>{keyboardSeeking=false;};
  Deactivated+=(_,_)=>CancelScrub();
  seek.AddHandler(PointerPressedEvent,(_,e)=>{if(armed||!e.GetCurrentPoint(seek).Properties.IsLeftButtonPressed)return;scrubbing=true;e.Pointer.Capture(seek);Scrub(e);e.Handled=true;},RoutingStrategies.Tunnel,true);
  seek.AddHandler(PointerMovedEvent,(_,e)=>{if(scrubbing){Scrub(e);e.Handled=true;}},RoutingStrategies.Tunnel,true);
  seek.AddHandler(PointerReleasedEvent,async(_,e)=>{if(!scrubbing)return;Scrub(e);scrubbing=false;e.Pointer.Capture(null);e.Handled=true;await SeekAsync();},RoutingStrategies.Tunnel,true);
  timer.Tick+=(_,_)=>TickStatus();
  AddHandler(KeyDownEvent,async(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;if(armed)await Safe(async()=>{await RequestStopAsync(false);});else await Safe(()=>Preview.StopAsync());}},RoutingStrategies.Tunnel);
  Opened+=async(_,_)=>await InitializeAsync();
  Closing+=OnWindowClosing;
 }
 void TracePointer(string kind,PointerEventArgs e)
 {
  if(Program.Option("--diagnostic-ui") is not {} file)return;
  var p=e.GetPosition(Grid);var hit=Grid.GetVisualAt(p) as Control;
  File.AppendAllText(file,System.Text.Json.JsonSerializer.Serialize(new{kind,x=p.X,y=p.Y,source=e.Source?.GetType().Name,hit=hit?.GetType().Name,context=hit?.DataContext?.GetType().Name,dragged=dragged?.Name,dragging,seek=Control<Slider>("Seek").Value})+Environment.NewLine);
 }
 string CueName(Guid? id)=>Editor.Rows.FirstOrDefault(x=>x.CueId==id)?.Name??"—";
 async Task InitializeAsync()
 {
  try {
   settings=InputSettings.Load(Program.Option("--settings"));
   bool loadDefault=Program.Option("--program")==null&&(!Program.Arguments.Any(a=>a.StartsWith("--verify-",StringComparison.Ordinal))||Program.Option("--verify-default-show")!=null);
   if(loadDefault&&!File.Exists(Program.Option("--settings")??InputSettings.PathName))settings=settings with{Rate="29.97 DF"};
   Editor.Rate=TimecodeRate.Modes.FirstOrDefault(r=>r.Name==settings.Rate)??TimecodeRate.Modes[1];
   LoadOutputSelection();
   await monitor.ConfigureAsync(settings);timer.Start();await Task.Delay(200);Surface.Black();Editor.Validate();StatusMessage("準備完了。動画を追加してください。");
   if(Program.Option("--program") is {} file)await Edit(()=>Editor.LoadAsync(file));
   else if(loadDefault)await Edit(()=>DefaultTestShow.LoadAsync(Editor));

#if VERIFICATION
   if(Program.Option("--verify-output-mode") is {} modeFolder)await OutputModeVerification.RunAsync(this,modeFolder);
   if(Program.Option("--verify-live-startup") is {} liveFolder)await LiveStartupVerification.RunAsync(this,liveFolder);
   if(Program.Option("--verify-resume-latency") is {} latencyFolder)await ResumeLatencyVerification.RunAsync(this,latencyFolder);
   if(Program.Option("--verify-portrait-monitor") is {} portraitFolder)await PortraitMonitorVerification.RunAsync(this,portraitFolder);
   if(Program.Option("--verify-release-features") is {} featureFolder)await ReleaseFeaturesVerification.RunAsync(this,featureFolder);
   if(Program.Option("--verify-sync-recovery-output") is {} syncFolder)await SyncRecoveryVerification.RunAsync(this,syncFolder);
   if(Program.Option("--verify-preview-performance") is {} performanceFolder)await PreviewPerformanceVerification.RunAsync(this,performanceFolder);
   if(Program.Option("--verify-phase2-adapter") is {} phase2Folder){await Phase2Verification.RunAsync(new RegressionWindow(),phase2Folder);Shutdown(RegressionApplication.Current.ExitCode);return;}
   if(Program.Option("--verify-phase5") is {} folder)await Phase5Verification.RunAsync(this,folder);
   if(Program.Option("--verify-monitor") is {} monitorFolder)await MonitorVerification.RunAsync(this,monitorFolder);
   if(Program.Option("--verify-ui-update") is {} updateFolder)await UiUpdateVerification.RunAsync(this,updateFolder);
   if(Program.Option("--verify-time-display") is {} displayFolder)DisplayFormatVerification.Run(this,displayFolder);
   if(Program.Option("--verify-preview-recovery") is {} recoveryFolder)await PreviewRecoveryVerification.RunAsync(this,recoveryFolder);
   if(Program.Option("--verify-delivery-operations") is {} deliveryFolder)await DeliveryOperationsVerification.RunAsync(this,deliveryFolder);
   if(Program.Option("--verify-default-show") is {} defaultFolder)await DefaultTestShow.VerifyAsync(this,defaultFolder);
#endif
  }catch(Exception e){StatusMessage(e.Message);var folder=Program.Option("--verify-phase5")??Program.Option("--verify-ui-update")??Program.Option("--verify-monitor")??Program.Option("--verify-preview-recovery")??Program.Option("--verify-delivery-operations")??Program.Option("--verify-default-show");if(folder!=null){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"fatal.txt"),e.ToString());Shutdown(1);}}
 }
 internal static void Shutdown(int code){if(Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d)d.Shutdown(code);}
 async Task Safe(Func<Task> action){try{await action();}catch(Exception e){StatusMessage(e.Message);}}
 async Task Edit(Func<Task> action)
 {
  if(busy||armed||closing)return;busy=true;UpdateEnabled();
  try{await action();if(settings.Rate!=Editor.Rate.Name){await Preview.StopAsync();settings=settings with{Rate=Editor.Rate.Name};await monitor.ConfigureAsync(settings);SaveSettings();}StatusMessage("編集を反映しました。");}
  catch(Exception e){StatusMessage(e.Message);}finally{busy=false;Editor.Validate();UpdateEnabled();}
 }
 async Task SelectPreviewAsync()
 {
  long version=++selectionVersion;
  if(armed||closing||rebinding||Grid.SelectedItem is not CueRow row)return;
  previewCue=row.CueId;CancelScrub();
  await Safe(async()=>{await Preview.LoadAsync(row.FilePath);if(version!=selectionVersion)return;UpdatePreview(Preview.Snapshot);});
 }
 void ApplyVolume()=>Preview?.SetVolume(Control<Slider>("Volume").Value,Control<CheckBox>("Mute").IsChecked==true);
 void UpdatePreview(PreviewSnapshot p)
 {
  UpdateRowControls();UpdateActivity();UpdatePreviewGeometry();if(armed)return;
  Control<TextBlock>("PreviewName").Text=p.Name==""?"キューを選択してください":p.Name;
  Control<TextBlock>("PreviewTime").Text=$"{MediaTimeDisplay.Format(p.Position,Preview.FrameRate)} / {MediaTimeDisplay.Format(p.Duration,Preview.FrameRate)}";
  Control<TextBlock>("PreviewInfo").Text=$"{p.State} / {p.Notice}";
  var slider=Control<Slider>("Seek");slider.Maximum=Math.Max(.001,p.Duration);
  if(!scrubbing&&!keyboardSeeking){updatingSeek=true;slider.Value=p.Position;updatingSeek=false;}
 }
 void UpdateActivity()
 {
  bool waiting=busy||Preview.IsBusy||delivery is DeliveryState.Starting or DeliveryState.Stopping;
  Control<ProgressBar>("ActivityBar").IsVisible=waiting;
  var label=Control<TextBlock>("ActivityLabel");label.IsVisible=waiting;
  label.Text=delivery==DeliveryState.Starting?"出力を準備中…":delivery==DeliveryState.Stopping?"出力を停止中…":Preview.IsBusy?Preview.BusyMessage:busy?"処理中…":"";
  Control<Slider>("Seek").IsEnabled=!Preview.IsBusy&&!busy&&!armed;
 }
 void UpdateEnabled()
 {
  bool edit=!armed&&!busy&&!closing;
  Control<StackPanel>("EditorTools").IsEnabled=edit;Grid.IsEnabled=edit;

  UpdateDeliveryButton();
  Control<StackPanel>("PreviewControls").IsVisible=!armed;UpdateRowControls();
 }
 async void AddClick(object? s,RoutedEventArgs e)
 {
  if(armed||busy)return;var files=await StorageProvider.OpenFilePickerAsync(new(){Title="動画を登録",AllowMultiple=true,FileTypeFilter=[new("対応動画"){Patterns=["*.mp4","*.mov"]}]});
  if(files.Count>0)await Edit(()=>Editor.ImportAsync(files.Select(f=>f.TryGetLocalPath()??throw new IOException("ローカルファイルを指定してください。")),new CueRow().StartTimecode));
 }
 async void DeleteClick(object? s,RoutedEventArgs e){if(Grid.SelectedItem is CueRow row)await Edit(async()=>{await Preview.StopAsync();Editor.Delete(row);});}
 async void CloneClick(object? s,RoutedEventArgs e){if(Grid.SelectedItem is CueRow row)await Edit(async()=>{RebindGrid(Editor.Clone(row));await SelectPreviewAsync();});}
 void RebindGrid(CueRow row)
 {
  rebinding=true;try{Grid.ItemsSource=null;Grid.ItemsSource=Editor.Rows;Grid.SelectedItem=row;}finally{rebinding=false;}
 }
 async void SaveClick(object? s,RoutedEventArgs e)
 {
  if(armed||busy)return;Grid.CommitEdit();var file=await StorageProvider.SaveFilePickerAsync(new(){Title="上演プログラム保存",DefaultExtension="json",FileTypeChoices=[new("上演プログラム"){Patterns=["*.json"]}]});
  if(file?.TryGetLocalPath() is {} path)await Edit(()=>Editor.SaveAsync(path));
 }
 async void LoadClick(object? s,RoutedEventArgs e)
 {
  if(armed||busy)return;var files=await StorageProvider.OpenFilePickerAsync(new(){Title="上演プログラム読込",FileTypeFilter=[new("上演プログラム"){Patterns=["*.json"]}]});
  if(files.FirstOrDefault()?.TryGetLocalPath() is {} path)await Edit(async()=>{await Preview.StopAsync();await Editor.LoadAsync(path);});
 }
 async void PreviewPlayClick(object? s,RoutedEventArgs e)=>await Safe(async()=>{if(Grid.SelectedItem is not CueRow row)throw new InvalidOperationException("キューを選択してください。");if(!string.Equals(Preview.LoadedPath,row.FilePath,StringComparison.OrdinalIgnoreCase))await Preview.LoadAsync(row.FilePath);await Preview.PlayAsync();});
 async void PreviewPauseClick(object? s,RoutedEventArgs e)=>await Safe(()=>Preview.PauseAsync());
 async void PreviewStopClick(object? s,RoutedEventArgs e)=>await Safe(()=>Preview.StopAsync());
 async Task SeekAsync(){if(!updatingSeek&&!armed)await Safe(()=>Preview.SeekAsync(Control<Slider>("Seek").Value));}
 async void SeekKeyUp(object? s,KeyEventArgs e){if(e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown){try{await SeekAsync();}finally{keyboardSeeking=false;UpdatePreview(Preview.Snapshot);}}}
 void HandlePressed(object? sender,PointerPressedEventArgs e){if(!armed&&!busy&&sender is Control c&&e.GetCurrentPoint(c).Properties.IsLeftButtonPressed){dragged=c.DataContext as CueRow;dragOrigin=e.GetPosition(Grid);dragging=false;e.Pointer.Capture(Grid);e.Handled=true;}}
 void HandleMoved(object? sender,PointerEventArgs e)
 {
  if(armed||busy||dragged==null||!e.GetCurrentPoint(Grid).Properties.IsLeftButtonPressed)return;
  var p=e.GetPosition(Grid);if(Math.Abs(p.X-dragOrigin.X)+Math.Abs(p.Y-dragOrigin.Y)<8)return;
  dragging=true;e.Handled=true;
 }
 void HandleReleased(object? sender,PointerReleasedEventArgs e)
 {
  if(dragged==null)return;var row=dragged;bool move=dragging;
  var hit=Grid.GetVisualAt(e.GetPosition(Grid)) as Control;
  var dest=hit?.GetSelfAndVisualAncestors().OfType<Control>().Select(c=>c.DataContext).OfType<CueRow>().FirstOrDefault();
  int from=Editor.Rows.IndexOf(row),target=dest==null?Editor.Rows.Count-1:Editor.Rows.IndexOf(dest);
  dragged=null;dragging=false;e.Pointer.Capture(null);e.Handled=true;
  if(Program.Option("--diagnostic-ui") is {} log)File.AppendAllText(log,System.Text.Json.JsonSerializer.Serialize(new{kind="reorder",from,target,move,busy,armed})+Environment.NewLine);
  if(armed||busy||!move)return;
  try{Editor.Move(row,target);RebindGrid(row);StatusMessage("表示順を変更しました。");}catch(Exception ex){StatusMessage(ex.Message);}
 }
 void DragOver(object? s,DragEventArgs e){e.DragEffects=!armed&&!busy&&e.DataTransfer.Formats.Contains(DataFormat.File)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
 async void Drop(object? s,DragEventArgs e)
 {
  if(armed||busy)return;e.Handled=true;
  if(e.DataTransfer.Formats.Contains(DataFormat.File)&&e.DataTransfer.TryGetFiles() is {} files){await Edit(()=>Editor.ImportAsync(files.Select(f=>f.TryGetLocalPath()).OfType<string>(),new CueRow().StartTimecode));return;}
 }
}


