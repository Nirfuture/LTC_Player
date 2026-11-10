using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using LtcPlayer.App;
using System.Globalization;
namespace LtcPlayer.Avalonia;
public static class StopDialog
{
 public static Task<bool> ShowAsync(Window owner,bool exit)
 {
  var keep=new Button{Content="出力を続ける",IsDefault=true,IsCancel=true};var stop=new Button{Content=exit?"出力を停止して終了":"出力を停止"};
  var dialog=new Window{Title="出力停止の確認",Width=450,Height=180,CanResize=false,Topmost=true,WindowStartupLocation=WindowStartupLocation.CenterOwner,
   Content=new StackPanel{Margin=new Thickness(22),Spacing=20,Children={new TextBlock{Text=exit?"出力を停止して終了しますか？":"出力を停止しますか？"},new StackPanel{Orientation=Orientation.Horizontal,Spacing=12,HorizontalAlignment=HorizontalAlignment.Right,Children={keep,stop}}}}};
  keep.Click+=(_,_)=>dialog.Close(false);stop.Click+=(_,_)=>dialog.Close(true);dialog.Opened+=(_,_)=>keep.Focus();dialog.KeyDown+=(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;dialog.Close(false);}};
  return dialog.ShowDialog<bool>(owner);
 }
}
public record SettingsResult(InputSettings Settings,OutputDisplay Display);
public sealed class SettingsDialog : Window
{
 internal ComboBox ModeSelector{get;}
 internal TextBlock ActiveModeLabel{get;}
 public SettingsDialog(InputSettings current,OutputDisplay? display,bool readOnly)
 {
  Title="設定";Width=680;Height=800;MinWidth=360;MinHeight=320;CanResize=true;Topmost=true;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var inputs=AudioInputService.Devices().ToList();if(current.DeviceId!=""&&!inputs.Any(x=>x.Id==current.DeviceId))inputs.Add(new(){Id=current.DeviceId,Name=current.DeviceName+" (未接続)"});
  var input=new ComboBox{ItemsSource=inputs,HorizontalAlignment=HorizontalAlignment.Stretch};input.SelectedIndex=Math.Max(0,inputs.FindIndex(x=>x.Id==current.DeviceId));
  var displays=VideoRenderer.Displays();var output=new ComboBox{ItemsSource=displays,HorizontalAlignment=HorizontalAlignment.Stretch,SelectedIndex=Math.Max(0,Array.FindIndex(displays,d=>d.Name==display?.Name))};
  ModeSelector=new ComboBox{Name="OutputMode",ItemsSource=new[]{OutputPresentation.Label(OutputPresentationMode.Standard),OutputPresentation.Label(OutputPresentationMode.Compatibility)},SelectedIndex=(int)current.OutputMode,HorizontalAlignment=HorizontalAlignment.Stretch};
  ActiveModeLabel=new TextBlock{Text="現在の表示方式："+OutputPresentation.Label(current.OutputMode),Foreground=global::Avalonia.Media.Brushes.LightSkyBlue};
  var channel=new ComboBox{ItemsSource=new[]{"L","R"},SelectedIndex=current.Channel};var rate=new ComboBox{ItemsSource=TimecodeRate.Modes,SelectedItem=TimecodeRate.Modes.First(r=>r.Name==current.Rate)};
  TextBox Box(double v)=>new(){Text=v.ToString(CultureInfo.InvariantCulture)};
  var offset=Box(current.Offset);var freewheel=Box(current.Freewheel);var resync=Box(current.Resync);var error=new TextBlock{TextWrapping=global::Avalonia.Media.TextWrapping.Wrap};
  var fields=new StackPanel{Spacing=8};
  void Add(string label,Control c){fields.Children.Add(new TextBlock{Text=label});fields.Children.Add(c);}
  Add("LTC入力",input);Add("入力チャンネル",channel);Add("LTC形式",rate);Add("映像出力",output);Add("表示方式",ModeSelector);fields.Children.Add(new TextBlock{Text="通常は標準を使用します。全画面出力が不安定な場合は互換を選んでください。",TextWrapping=global::Avalonia.Media.TextWrapping.Wrap});Add("補正（フレーム）",offset);Add("入力喪失時の補間（秒）",freewheel);Add("再同期の閾値（フレーム）",resync);
  string coverPath=current.CoverPath;
  var coverName=new TextBlock{Text=coverPath==""?"未登録（黒）":Path.GetFileName(coverPath),TextWrapping=global::Avalonia.Media.TextWrapping.Wrap};
  var chooseCover=new Button{Content="画像を選択"};var clearCover=new Button{Content="解除"};
  var coverButtons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Children={chooseCover,clearCover}};
  Add("蓋画",coverName);fields.Children.Add(coverButtons);
  chooseCover.Click+=async(_,_)=>{var files=await StorageProvider.OpenFilePickerAsync(new(){Title="蓋画を登録",AllowMultiple=false,FileTypeFilter=[new("PNG・JPEG"){Patterns=["*.png","*.jpg","*.jpeg"]}]});if(files.FirstOrDefault()?.TryGetLocalPath() is {} path){try{await CoverImageLoader.LoadAsync(path);coverPath=path;coverName.Text=Path.GetFileName(path);error.Text="";}catch(Exception e){error.Text=e.Message;}}};
  clearCover.Click+=(_,_)=>{coverPath="";coverName.Text="未登録（黒）";};
  fields.IsEnabled=!readOnly;
  var apply=new Button{Content="適用",IsEnabled=!readOnly};var back=new Button{Content=readOnly?"閉じる":"キャンセル",IsCancel=true};
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right,Children={back,apply}};
  var layout=new Grid{Margin=new Thickness(20),RowDefinitions=new RowDefinitions("Auto,Auto,*,Auto,Auto")};
  ActiveModeLabel.Margin=new Thickness(0,0,0,10);layout.Children.Add(ActiveModeLabel);
  var hint=new TextBlock{Text=readOnly?"出力中は設定を変更できません。":"「適用」で設定を反映します。",Margin=new Thickness(0,0,0,10),TextWrapping=global::Avalonia.Media.TextWrapping.Wrap};Grid.SetRow(hint,1);layout.Children.Add(hint);
  var scroll=new ScrollViewer{Content=fields,HorizontalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,VerticalScrollBarVisibility=global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto};Grid.SetRow(scroll,2);layout.Children.Add(scroll);
  Grid.SetRow(error,3);error.Margin=new Thickness(0,8,0,0);layout.Children.Add(error);
  Grid.SetRow(buttons,4);buttons.Margin=new Thickness(0,12,0,0);layout.Children.Add(buttons);Content=layout;
  WindowPlacement.Track(this);
  back.Click+=(_,_)=>Close(null);
  apply.Click+=(_,_)=>{try{
   if(input.SelectedItem is not AudioDevice i||output.SelectedItem is not OutputDisplay d||rate.SelectedItem is not TimecodeRate m)throw new InvalidDataException("入出力を選択してください。");
   var updated=current with{CoverPath=coverPath,OutputMode=(OutputPresentationMode)ModeSelector.SelectedIndex,DeviceId=i.Id,DeviceName=i.Name,Channel=channel.SelectedIndex,Rate=m.Name,Offset=int.Parse(offset.Text!,CultureInfo.InvariantCulture),Freewheel=double.Parse(freewheel.Text!,CultureInfo.InvariantCulture),Resync=double.Parse(resync.Text!,CultureInfo.InvariantCulture)};
   OutputPresentation.Validate(updated.OutputMode);_=new MasterClock(m,updated.Freewheel,updated.Resync);Close(new SettingsResult(updated,d));
  }catch(Exception e){error.Text=e.Message;}};
 }
}
