using Avalonia.Controls;
namespace LtcPlayer.Avalonia;
public partial class MainWindow
{
 internal static string GeometryLabel(int width,int height){
  if(width<=0||height<=0)return "—";
  int a=width,b=height;while(b!=0){int n=a%b;a=b;b=n;}
  return $"{width}×{height}（{width/a}:{height/a}）";
 }
 internal void UpdatePreviewGeometry(){
  var viewport=Control<Grid>("PreviewViewport");var frame=Control<Border>("OutputFrame");
  bool output=armed&&selectedDisplay.HasValue;
  int width=output?selectedDisplay!.Value.Width:16,height=output?selectedDisplay!.Value.Height:9;
  double availableWidth=Math.Max(1,viewport.Bounds.Width-16),availableHeight=Math.Max(1,viewport.Bounds.Height-16);
  double scale=Math.Min(availableWidth/width,availableHeight/height);
  frame.Width=width*scale+4;frame.Height=height*scale+4;
  Control<TextBlock>("OutputGeometry").Text=selectedDisplay is {} display?"出力画面："+GeometryLabel(display.Width,display.Height):"出力画面：未選択";
  var mode=armed&&observation!=null?observation.OutputMode:settings.OutputMode;
  Control<TextBlock>("OutputModeLabel").Text="表示方式："+LtcPlayer.App.OutputPresentation.Label(mode);
  var cue=armed?Editor.Rows.FirstOrDefault(r=>r.CueId==observation?.CueId):null;
  var video=cue!=null?(cue.PixelWidth,cue.PixelHeight):armed?(0,0):Preview.MediaSize;
  Control<TextBlock>("VideoGeometry").Text="動画："+GeometryLabel(video.Item1,video.Item2);
  ToolTip.SetTip(frame,output?"青枠は出力画面全体です。黒い余白も本番出力に含まれます。":"確認用プレビュー");
 }
}
