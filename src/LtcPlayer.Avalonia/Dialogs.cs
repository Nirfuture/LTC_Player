using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
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
 public SettingsDialog(InputSettings current,OutputDisplay? display,bool readOnly)
 {
  Title="設定";Width=680;Height=750;CanResize=false;Topmost=true;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var inputs=AudioInputService.Devices().ToList();if(current.DeviceId!=""&&!inputs.Any(x=>x.Id==current.DeviceId))inputs.Add(new(){Id=current.DeviceId,Name=current.DeviceName+" (未接続)"});
  var input=new ComboBox{ItemsSource=inputs,HorizontalAlignment=HorizontalAlignment.Stretch};input.SelectedIndex=Math.Max(0,inputs.FindIndex(x=>x.Id==current.DeviceId));
  var displays=VideoRenderer.Displays();var output=new ComboBox{ItemsSource=displays,HorizontalAlignment=HorizontalAlignment.Stretch,SelectedIndex=Math.Max(0,Array.FindIndex(displays,d=>d.Name==display?.Name))};
  var channel=new ComboBox{ItemsSource=new[]{"L","R"},SelectedIndex=current.Channel};var rate=new ComboBox{ItemsSource=TimecodeRate.Modes,SelectedItem=TimecodeRate.Modes.First(r=>r.Name==current.Rate)};
  TextBox Box(double v)=>new(){Text=v.ToString(CultureInfo.InvariantCulture)};
  var offset=Box(current.Offset);var freewheel=Box(current.Freewheel);var resync=Box(current.Resync);var error=new TextBlock{TextWrapping=global::Avalonia.Media.TextWrapping.Wrap};
  var fields=new StackPanel{Spacing=8};
  void Add(string label,Control c){fields.Children.Add(new TextBlock{Text=label});fields.Children.Add(c);}
  Add("LTC入力",input);Add("入力チャンネル",channel);Add("LTC形式",rate);Add("映像出力",output);Add("補正（フレーム）",offset);Add("入力喪失時の補間（秒）",freewheel);Add("再同期の閾値（フレーム）",resync);
  fields.IsEnabled=!readOnly;
  var apply=new Button{Content="適用",IsEnabled=!readOnly};var back=new Button{Content=readOnly?"閉じる":"キャンセル",IsCancel=true};
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right,Children={back,apply}};
  Content=new StackPanel{Margin=new Thickness(20),Spacing=12,Children={new TextBlock{Text=readOnly?"出力中は設定を変更できません。":"適用するまで現在の入力は変更されません。"},new ScrollViewer{Height=530,Content=fields},error,buttons}};
  back.Click+=(_,_)=>Close(null);
  apply.Click+=(_,_)=>{try{
   if(input.SelectedItem is not AudioDevice i||output.SelectedItem is not OutputDisplay d||rate.SelectedItem is not TimecodeRate m)throw new InvalidDataException("入出力を選択してください。");
   var updated=current with{DeviceId=i.Id,DeviceName=i.Name,Channel=channel.SelectedIndex,Rate=m.Name,Offset=int.Parse(offset.Text!,CultureInfo.InvariantCulture),Freewheel=double.Parse(freewheel.Text!,CultureInfo.InvariantCulture),Resync=double.Parse(resync.Text!,CultureInfo.InvariantCulture)};
   _=new MasterClock(m,updated.Freewheel,updated.Resync);Close(new SettingsResult(updated,d));
  }catch(Exception e){error.Text=e.Message;}};
 }
}
